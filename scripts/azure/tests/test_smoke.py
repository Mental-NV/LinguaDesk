import io
import json
from email.message import Message
from pathlib import Path
import sys
import unittest
from unittest.mock import Mock, patch
import urllib.error
import urllib.parse

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import control
import deploy

URL = 'https://linguadesk.azurewebsites.net'
SHA = 'a' * 40
PRIVATE = 'private-response-and-cookie-sentinel'


class Response:
    def __init__(self, url, status=200, body=None, cookie=True):
        self.url = url
        self.status = status
        path = urllib.parse.urlparse(url).path
        self.body = body if body is not None else (json.dumps({'sha':SHA}).encode() if path == '/revision.json'
                    else b'<html>SPA</html>' if path in ('/', '/translate') else b'{}')
        self.headers = Message()
        if cookie:
            self.headers.add_header('Set-Cookie', '__Host-LinguaDesk.Antiforgery=' + PRIVATE + '; Secure; HttpOnly; Path=/')

    def __enter__(self): return self
    def __exit__(self, *args): pass
    def read(self, *args): return self.body
    def geturl(self): return self.url


class SmokeTests(unittest.TestCase):
    def run_smoke(self, response, probe=None):
        self.now = 0
        def sleep(seconds): self.now += seconds
        with patch.object(control.time,'monotonic',side_effect=lambda: self.now), \
             patch.object(control.time,'sleep',side_effect=sleep), \
             patch.object(control.urllib.request,'urlopen',side_effect=response) as requests:
            control.smoke(URL, SHA, state_probe=probe)
        return requests

    def valid(self, request, **kwargs):
        path = urllib.parse.urlparse(request.full_url).path
        if path in ('/api/m047-missing', '/assets/m047-missing.js'):
            raise urllib.error.HTTPError(request.full_url,404,'Not Found',Message(),io.BytesIO(PRIVATE.encode()))
        return Response(request.full_url)

    def test_success_covers_every_boundary_without_processing_or_state_probe(self):
        probe = Mock()
        requests = self.run_smoke(self.valid, probe)
        self.assertEqual(len(requests.call_args_list),10)
        self.assertFalse(any('/api/operations' in c.args[0].full_url for c in requests.call_args_list))
        probe.assert_not_called()

    def test_quota_after_start_aborts_first_failed_http_request(self):
        requests = []
        def forbidden(request, **kwargs):
            requests.append(request.full_url)
            raise urllib.error.HTTPError(request.full_url,403,'Forbidden',Message(),io.BytesIO(PRIVATE.encode()))
        probe = lambda: deploy.check_runtime_quota({'state':'QuotaExceeded'})
        with self.assertRaisesRegex(ValueError, 'AppServiceRuntimeQuotaExceeded') as caught:
            self.run_smoke(forbidden, probe)
        self.assertEqual(requests,[URL+'/health/live'])
        self.assertEqual(self.now,0)
        self.assertNotIn(PRIVATE,str(caught.exception))

    def test_persistent_http_failure_identifies_endpoint_and_throttles_state_reads(self):
        def unavailable(request, **kwargs):
            if request.full_url.endswith('/health/ready'):
                return Response(request.full_url,503,PRIVATE.encode())
            return self.valid(request,**kwargs)
        probe = Mock()
        with self.assertRaisesRegex(ValueError,r'/health/ready: HTTP 503; expected 200') as caught:
            self.run_smoke(unavailable,probe)
        self.assertEqual(self.now,240)
        self.assertLessEqual(probe.call_count,8)
        self.assertGreater(probe.call_count,0)
        self.assertNotIn(PRIVATE,str(caught.exception))

    def test_invalid_revision_cookie_or_redirect_remain_blocking_and_private(self):
        for case, expected in (('revision','/revision.json: invalid JSON or deployed revision mismatch'),
                               ('cookie','missing Secure/HttpOnly antiforgery cookie'),
                               ('redirect','unexpected redirect')):
            def invalid(request, **kwargs):
                if case == 'revision' and request.full_url.endswith('/revision.json'):
                    return Response(request.full_url,body=json.dumps({'sha':PRIVATE}).encode())
                if case == 'cookie' and request.full_url.endswith('/api/accounts/antiforgery'):
                    return Response(request.full_url,cookie=False)
                if case == 'redirect': return Response(URL+'/'+PRIVATE)
                return self.valid(request,**kwargs)
            with self.subTest(case=case), self.assertRaisesRegex(ValueError,expected) as caught:
                self.run_smoke(invalid)
            self.assertNotIn(PRIVATE,str(caught.exception))

    def test_network_error_does_not_leak_raw_exception(self):
        def fail(request, **kwargs): raise urllib.error.URLError(PRIVATE)
        with self.assertRaisesRegex(ValueError,'/health/live: network/timeout') as caught:
            self.run_smoke(fail)
        self.assertNotIn(PRIVATE,str(caught.exception))

    def test_transient_unavailability_recovers_without_weakening_the_smoke(self):
        count = 0
        def transient(request, **kwargs):
            nonlocal count
            count += 1
            if count == 1: return Response(request.full_url,503)
            return self.valid(request,**kwargs)
        probe = Mock()
        self.run_smoke(transient,probe)
        probe.assert_called_once()
        self.assertEqual(self.now,5)
