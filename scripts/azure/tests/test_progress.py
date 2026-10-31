import contextlib
import io
import json
from pathlib import Path
import select
import subprocess
import sys
import threading
import unittest
from unittest.mock import patch

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import deploy
import progress


class ProgressTests(unittest.TestCase):
    def test_messages_are_flushed_without_timestamps(self):
        with patch('builtins.print') as output:
            progress.log('Checking readiness.')
        output.assert_called_once_with('[deploy] Checking readiness.', flush=True)

    def test_piped_process_streams_progress_before_finishing(self):
        program = "from progress import stage\nimport sys\nwith stage('Activation', heartbeat_seconds=0.02):\n    sys.stdin.readline()\n"
        process = subprocess.Popen([sys.executable, '-c', program], cwd=Path(__file__).resolve().parents[1],
            stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=subprocess.PIPE, bufsize=0)
        try:
            self.assertTrue(select.select([process.stdout], [], [], 2)[0], 'Stage start must be visible on a pipe immediately.')
            self.assertEqual(process.stdout.readline(), b'[deploy] Starting: Activation.\n')
            self.assertTrue(select.select([process.stdout], [], [], 2)[0], 'Heartbeat must be visible before the process exits.')
            self.assertTrue(process.stdout.readline().startswith(b'[deploy] Waiting: Activation ('))
            self.assertIsNone(process.poll())
            output, error = process.communicate(b'\n', timeout=2)
            self.assertEqual(process.returncode, 0, error)
            self.assertIn(b'[deploy] Completed: Activation (', output)
        finally:
            if process.poll() is None:
                process.kill()
            process.communicate(timeout=2)

    def test_heartbeat_is_visible_while_stage_is_still_waiting(self):
        waiting = threading.Event()
        messages = []
        def record(message):
            messages.append(message)
            if message.startswith('Waiting:'):
                waiting.set()
        with patch.object(progress, 'log', side_effect=record):
            with progress.stage('Activate runtime settings', heartbeat_seconds=0.01):
                self.assertTrue(waiting.wait(2), 'Long-running stages must emit progress before completing.')
                self.assertFalse(any(message.startswith('Completed:') for message in messages))
        self.assertEqual(messages[0], 'Starting: Activate runtime settings.')
        self.assertTrue(messages[-1].startswith('Completed: Activate runtime settings ('))

    def test_failed_stage_preserves_failure_without_printing_exception_details(self):
        output = io.StringIO()
        private = 'private-error-detail-sentinel'
        with contextlib.redirect_stdout(output):
            with self.assertRaisesRegex(ValueError, private):
                with progress.stage('Apply infrastructure'):
                    raise ValueError(private)
        self.assertIn('Failed: Apply infrastructure', output.getvalue())
        self.assertNotIn('Completed:', output.getvalue())
        self.assertNotIn(private, output.getvalue())

    def test_progress_never_streams_azure_parameters_or_captured_output(self):
        private = 'private-parameter-and-output-sentinel'
        for exit_code in (0, 1):
            output = io.StringIO()
            result = subprocess.CompletedProcess('az', exit_code, json.dumps({'private': private}),
                json.dumps({'code': 'Conflict', 'message': private}))
            with self.subTest(exit_code=exit_code), contextlib.redirect_stdout(output), \
                 patch.object(deploy.subprocess, 'run', return_value=result) as command:
                if exit_code:
                    with self.assertRaisesRegex(ValueError, 'Conflict'):
                        with progress.stage('Activate runtime settings'):
                            deploy.az('deployment', 'group', 'create', '--parameters', private)
                else:
                    with progress.stage('Activate runtime settings'):
                        self.assertEqual(deploy.az('deployment', 'group', 'create', '--parameters', private), {'private': private})
            self.assertTrue(command.call_args.kwargs['capture_output'])
            self.assertNotIn(private, output.getvalue())


if __name__ == '__main__': unittest.main()
