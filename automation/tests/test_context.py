"""Behavioral checks for context selection, freshness and safe runner gating."""
import contextlib
import importlib.util
import io
import json
import os
from pathlib import Path
import shutil
import subprocess
import tempfile
import unittest
from unittest.mock import patch

MODULE = Path(__file__).resolve().parents[1] / 'context.py'
spec = importlib.util.spec_from_file_location('context', MODULE)
context = importlib.util.module_from_spec(spec)
spec.loader.exec_module(context)

MUSE_STREAM_MODULE = MODULE.parent / 'muse_stream.py'
muse_stream_spec = importlib.util.spec_from_file_location(
    'muse_stream', MUSE_STREAM_MODULE,
)
muse_stream = importlib.util.module_from_spec(muse_stream_spec)
muse_stream_spec.loader.exec_module(muse_stream)


class FlushingBuffer(io.StringIO):
    def __init__(self):
        super().__init__()
        self.flushes = 0

    def flush(self):
        self.flushes += 1
        super().flush()


class MuseStreamTests(unittest.TestCase):
    def test_deltas_stream_as_plain_text_without_terminal_duplication(self):
        events = [
            'muse: workspace trusted\n',
            json.dumps({
                'payload_type': 'run.output.delta',
                'payload': {'text': 'working...'},
            }) + '\n',
            json.dumps({
                'payload_type': 'run.output.delta',
                'payload': {'text': '\nMILESTONE_AUTOMATION_STATUS: READY'},
            }) + '\n',
            json.dumps({
                'payload_type': 'run.terminal.completed',
                'payload': {
                    'text': 'working...\nMILESTONE_AUTOMATION_STATUS: READY',
                },
            }) + '\n',
        ]
        output = FlushingBuffer()

        muse_stream.render(events, output)

        self.assertEqual(
            output.getvalue(),
            'muse: workspace trusted\nworking...\n'
            'MILESTONE_AUTOMATION_STATUS: READY\n',
        )
        self.assertGreaterEqual(output.flushes, 4)

    def test_terminal_text_is_used_when_no_deltas_are_available(self):
        event = json.dumps({
            'payload_type': 'run.terminal.completed',
            'payload': {'text': 'final only'},
        }) + '\n'
        output = FlushingBuffer()

        muse_stream.render([event], output)

        self.assertEqual(output.getvalue(), 'final only\n')

    def test_lifecycle_and_tool_events_render_live_progress(self):
        events = [
            json.dumps({
                'payload_type': 'task.lifecycle.proposed',
                'payload': {'event': {'task_kind': 'model.meta.response'}},
            }) + '\n',
            json.dumps({
                'payload_type': 'task.lifecycle.proposed',
                'payload': {'event': {'task_kind': 'tool.bash'}},
            }) + '\n',
            json.dumps({
                'payload_type': 'tool.result',
                'payload': {
                    'text': json.dumps({
                        'command': 'printf super-secret',
                        'description': 'Check the project',
                        'terminal_status': 'completed',
                        'exit_code': 0,
                        'output': 'super-secret',
                    }),
                    'correlation_facts': {
                        'tool_name': 'bash',
                        'outcome': 'success',
                    },
                },
            }) + '\n',
        ]
        output = FlushingBuffer()

        muse_stream.render(events, output)

        self.assertEqual(
            output.getvalue(),
            '[muse] Model step started.\n'
            '[muse] Running bash...\n'
            '[muse] Check the project — completed (exit 0)\n',
        )
        self.assertNotIn('super-secret', output.getvalue())

    def test_provider_failure_events_are_visible(self):
        events = [
            json.dumps({
                'payload_type': 'task.lifecycle.status',
                'payload': {'event': {
                    'message': 'failed meta model stream attempt 1/10',
                    'details': {'phase': 'stream_failed'},
                }},
            }) + '\n',
            json.dumps({
                'payload_type': 'run.terminal.failed',
                'payload': {'reason': 'server_error: error code: 504'},
            }) + '\n',
        ]
        output = FlushingBuffer()

        muse_stream.render(events, output)

        self.assertEqual(
            output.getvalue(),
            '[muse] failed meta model stream attempt 1/10\n'
            '[muse] Run failed: server_error: error code: 504\n',
        )


class ContextTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.root = Path(self.temp.name).resolve()
        self.patch = patch.object(context, 'ROOT', self.root)
        self.patch.start()
        self.addCleanup(self.patch.stop)
        self.addCleanup(self.temp.cleanup)
        for name in context.RULES:
            self.write(name, '# Rules\nStable rules.\n')
        self.directory = 'docs/08-backlogs/M015/'
        for name in ('backlog.md', 'spec.md', 'plan.md', 'tasks.md'):
            self.write(self.directory + name, '# ' + name + '\nOriginal content.\n')
        self.write('docs/source.md', '# Source\n## Selected\nCanonical rule.\n### Child\nChild rule.\n## Unrelated\nOutside scope.\n')
        self.write('docs/adr.md', '# Rationale\nONLY_ON_DEMAND\n')
        self.data = {'version': 1, 'scope': 'M015', 'sources': [
            {'path': 'docs/source.md', 'heading': 'Selected', 'use': 'excerpt', 'reason': 'Behavior'},
            {'path': 'docs/adr.md', 'use': 'reference', 'reason': 'Boundary redesign'}]}
        self.save()

    def write(self, path, text):
        dest = self.root / path
        dest.parent.mkdir(parents=True, exist_ok=True)
        dest.write_text(text)

    def save(self):
        self.write(self.directory + 'context.json', json.dumps(self.data))

    def lock(self):
        with patch.object(context.subprocess, 'check_output', return_value='a' * 40), contextlib.redirect_stdout(io.StringIO()):
            context.lock('M015')

    def test_selected_parent_includes_children_not_siblings(self):
        value = context.excerpt(self.data['sources'][0])
        self.assertIn('Child rule', value)
        self.assertNotIn('Outside scope', value)

    def test_exact_id_rows_do_not_match_longer_ids_or_prose(self):
        self.write('docs/rows.md', '# Rows\nFR-001 is mentioned.\n| FR-001 | yes |\n| FR-0010 | no |\n')
        value = context.excerpt({'path': 'docs/rows.md', 'ids': ['FR-001']})
        self.assertEqual(value, '| FR-001 | yes |\n')
        with self.assertRaises(ValueError):
            context.excerpt({'path': 'docs/rows.md', 'ids': ['FR-002']})

    def test_ambiguous_heading_or_id_fails(self):
        self.write('docs/rows.md', '# Rows\n## Duplicate\nx\n## Duplicate\ny\n| FR-001 | a |\n| FR-001 | b |\n')
        for selector in ({'heading': 'Duplicate'}, {'ids': ['FR-001']}):
            with self.assertRaises(ValueError):
                context.excerpt({'path': 'docs/rows.md', **selector})

    def test_fenced_examples_are_not_headings(self):
        self.write('docs/fenced.md', '```md\n## Selected\nexample\n```\n## Selected\nreal\n')
        self.assertEqual(context.excerpt({'path': 'docs/fenced.md', 'heading': 'Selected'}), '## Selected\nreal\n')

    def test_unrelated_source_edit_and_task_progress_keep_lock(self):
        self.lock()
        file = self.root / 'docs/source.md'
        file.write_text(file.read_text().replace('Outside scope.', 'Changed unrelated section.'))
        self.write(self.directory + 'tasks.md', '# Tasks\n- [x] T001\n')
        context.check('M015')

    def test_selected_child_edit_invalidates(self):
        self.lock()
        file = self.root / 'docs/source.md'
        file.write_text(file.read_text().replace('Child rule.', 'New child rule.'))
        with self.assertRaisesRegex(ValueError, 'changed/unlocked source'):
            context.check('M015')

    def test_plan_and_governance_edits_invalidate(self):
        for path in (self.directory + 'plan.md', context.RULES[0]):
            with self.subTest(path=path):
                self.lock()
                self.write(path, '# Changed\n')
                with self.assertRaisesRegex(ValueError, 'changed/unlocked'):
                    context.check('M015')

    def test_manifest_selection_change_invalidates(self):
        self.lock()
        path = self.root / self.directory / 'context.json'
        data = json.loads(path.read_text())
        data['sources'][0]['use'] = 'reference'
        path.write_text(json.dumps(data))
        with self.assertRaisesRegex(ValueError, 'source map changed'):
            context.check('M015')

    def test_deleted_on_demand_source_blocks(self):
        self.lock()
        (self.root / 'docs/adr.md').unlink()
        with self.assertRaises(ValueError):
            context.check('M015')

    def test_packet_stable_first_mutable_last_and_no_on_demand_content(self):
        self.lock()
        packet = context.packet('M015')
        self.assertNotIn('ONLY_ON_DEMAND', packet)
        self.assertIn('docs/adr.md', packet)
        positions = [packet.index(x) for x in ('Stable rules.', 'Canonical rule.', 'Selected scope:', '# spec.md', '# plan.md', '# backlog.md', '# tasks.md')]
        self.assertEqual(positions, sorted(positions))
        self.assertEqual(packet, context.packet('M015'))

    def test_missing_lock_prevents_packet(self):
        with self.assertRaises(ValueError):
            context.packet('M015')

    def test_path_escape_and_invalid_scope_fail(self):
        with self.assertRaises(ValueError):
            context.path_for('../not-in-repository.md')
        with self.assertRaises(ValueError):
            context.package('../M015')
        self.data['sources'][0]['path'] = '../source.md'
        self.save()
        with self.assertRaises(ValueError):
            context.manifest('M015')

    def test_missing_or_broken_link_and_anchor_fail_audit(self):
        self.write('README.md', '[bad](docs/source.md#missing)\n')
        with self.assertRaisesRegex(ValueError, 'missing anchor'):
            context.audit()
        self.write('README.md', '[bad](docs/missing.md)\n')
        with self.assertRaisesRegex(ValueError, 'missing docs/missing'):
            context.audit()
        self.write('README.md', '[good](docs/source.md#selected)\n```md\n[example](missing.md)\n```\n')
        with contextlib.redirect_stdout(io.StringIO()):
            context.audit()


class RunnerTests(unittest.TestCase):
    @contextlib.contextmanager
    def fixture(self):
        # Real runner + fake coding CLI in an isolated repository. No paid calls
        # or user-repository commits/application effects occur.
        with tempfile.TemporaryDirectory() as temp, tempfile.TemporaryDirectory() as remote_temp:
            root = Path(temp).resolve()
            remote = Path(remote_temp).resolve() / 'origin.git'
            (root / 'automation').mkdir()
            for name in (
                'context.py', 'run-milestones.sh', 'muse_stream.py',
                'plan.prompt.md', 'implement.prompt.md',
            ):
                shutil.copyfile(MODULE.parent / name, root / 'automation' / name)
            (root / 'docs/08-backlogs').mkdir(parents=True)
            (root / 'docs/07-roadmap.md').write_text('| M015 | Test |\n')
            (root / 'README.md').write_text('# Test\n')
            (root / 'backend').mkdir()
            (root / 'backend/LinguaDesk.slnx').write_text('test')
            (root / 'scripts').mkdir()
            for name in ('backend.sh', 'contract.sh', 'ai.sh', 'frontend.sh'):
                file = root / 'scripts' / name
                file.write_text('#!/bin/sh\nexit 0\n')
                file.chmod(0o755)
            (root / 'fake-bin').mkdir()
            bodies = {
                'dotnet': 'exit 0',
                'codex': (
                    'if [ "${CODEX_FIXTURE_MODE:-}" = "complete" ]; then\n'
                    '  echo implementation >> README.md\n'
                    '  echo MILESTONE_AUTOMATION_STATUS: COMPLETE\n'
                    'else\n'
                    '  echo MILESTONE_AUTOMATION_STATUS: READY\n'
                    'fi'
                ),
                'muse': (
                    'count=1\n'
                    'if [ -n "${MUSE_FIXTURE_COUNT_FILE:-}" ]; then\n'
                    '  if [ -f "$MUSE_FIXTURE_COUNT_FILE" ]; then count=$(( $(cat "$MUSE_FIXTURE_COUNT_FILE") + 1 )); fi\n'
                    '  echo "$count" > "$MUSE_FIXTURE_COUNT_FILE"\n'
                    'fi\n'
                    'if [ "${MUSE_FIXTURE_MODE:-}" = "transient-once" ] && [ "$count" -eq 1 ]; then\n'
                    "  echo '{\"payload_type\":\"run.terminal.failed\",\"payload\":{\"reason\":\"server_error: error code: 504\"}}'\n"
                    '  exit 1\n'
                    'fi\n'
                    'if [ "${MUSE_FIXTURE_MODE:-}" = "stale-status" ] && [ "$count" -eq 1 ]; then\n'
                    "  echo '{\"payload_type\":\"run.output.delta\",\"payload\":{\"text\":\"MILESTONE_AUTOMATION_STATUS: READY\"}}'\n"
                    "  echo '{\"payload_type\":\"run.terminal.failed\",\"payload\":{\"reason\":\"server_error: error code: 504\"}}'\n"
                    '  exit 1\n'
                    'fi\n'
                    'if [ "${MUSE_FIXTURE_MODE:-}" = "stale-status" ]; then\n'
                    "  echo '{\"payload_type\":\"run.output.delta\",\"payload\":{\"text\":\"finished without a status\"}}'\n"
                    '  exit 0\n'
                    'fi\n'
                    'if [ "${MUSE_FIXTURE_MODE:-}" = "permanent" ]; then\n'
                    "  echo '{\"payload_type\":\"run.terminal.failed\",\"payload\":{\"reason\":\"invalid request\"}}'\n"
                    '  exit 1\n'
                    'fi\n'
                    'echo \"muse-harness-args: $*\" >&2\n'
                    "echo '{\"payload_type\":\"task.lifecycle.proposed\",\"payload\":{\"event\":{\"task_kind\":\"model.meta.response\"}}}'\n"
                    "echo '{\"payload_type\":\"run.output.delta\",\"payload\":{\"text\":\"muse working...\\nMILESTONE_AUTOMATION_STATUS: READY\"}}'"
                ),
            }
            for name, body in bodies.items():
                file = root / 'fake-bin' / name
                file.write_text('#!/bin/sh\n' + body + '\n')
                file.chmod(0o755)
            env = dict(os.environ, PATH=str(root / 'fake-bin') + os.pathsep + os.environ['PATH'])
            def git(*args):
                return subprocess.check_output(['git', '-C', str(root), *args], text=True, stderr=subprocess.DEVNULL)
            git('init')
            git('config', 'user.email', 'fixture@example.invalid')
            git('config', 'user.name', 'Fixture')
            git('add', '.')
            git('commit', '-m', 'fixture')
            git('branch', '-M', 'main')
            subprocess.check_call(
                ['git', 'init', '--bare', str(remote)],
                stdout=subprocess.DEVNULL,
                stderr=subprocess.DEVNULL,
            )
            git('remote', 'add', 'origin', str(remote))
            git('push', '--set-upstream', 'origin', 'main')
            yield root, env, git

    def run_fixture(self, root, env, *extra):
        return subprocess.run(['bash', str(root / 'automation/run-milestones.sh'), *extra, '15', '15'], env=env, text=True, capture_output=True)

    def prepare_plan(self, root, git):
        for name in context.RULES:
            path = root / name
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text('# Rules\nStable rules.\n')
        package = root / 'docs/08-backlogs/M015'
        package.mkdir()
        for name in ('backlog.md', 'spec.md', 'plan.md', 'tasks.md'):
            (package / name).write_text('# Package\nSelected fixture.\n')
        (root / 'docs/source.md').write_text('# Contract\nOriginal rule.\n')
        (package / 'context.json').write_text(json.dumps({'version': 1, 'scope': 'M015', 'sources': [
            {'path': 'docs/source.md', 'use': 'excerpt', 'reason': 'Fixture contract'}]}))
        with patch.object(context, 'ROOT', root), contextlib.redirect_stdout(io.StringIO()):
            context.lock('M015')
        git('add', '.')
        git('commit', '-m', 'M015 planned')

    def test_ready_without_lock_cannot_commit_or_implement(self):
        with self.fixture() as (root, env, git):
            before = git('rev-parse', 'HEAD')
            result = self.run_fixture(root, env)
            self.assertNotEqual(result.returncode, 0)
            self.assertIn('MILESTONE_AUTOMATION_STATUS: READY', result.stdout, result.stderr)
            self.assertNotIn('Implementing M015', result.stdout)
            self.assertEqual(before, git('rev-parse', 'HEAD'))

    def test_current_planning_commit_is_reused(self):
        with self.fixture() as (root, env, git):
            self.prepare_plan(root, git)
            before = git('rev-parse', 'HEAD')
            result = self.run_fixture(root, env)
            self.assertIn('Reusing it.', result.stdout, result.stderr)
            self.assertIn('Implementing M015', result.stdout)
            self.assertNotIn('Planning M015', result.stdout)
            # Fake implementation reports READY, not COMPLETE, so no commit follows.
            self.assertNotEqual(result.returncode, 0)
            self.assertEqual(before, git('rev-parse', 'HEAD'))

    def test_stale_plan_replans_and_cannot_reuse_old_lock(self):
        with self.fixture() as (root, env, git):
            self.prepare_plan(root, git)
            (root / 'docs/source.md').write_text('# Contract\nChanged rule.\n')
            git('add', '.')
            git('commit', '-m', 'Changed contract')
            before = git('rev-parse', 'HEAD')
            result = self.run_fixture(root, env)
            self.assertIn('Planning M015', result.stdout, result.stderr)
            self.assertNotIn('Implementing M015', result.stdout)
            self.assertNotEqual(result.returncode, 0)
            self.assertEqual(before, git('rev-parse', 'HEAD'))

    def test_muse_ready_without_lock_cannot_commit_or_implement(self):
        with self.fixture() as (root, env, git):
            before = git('rev-parse', 'HEAD')
            result = self.run_fixture(root, env, '--muse')
            self.assertNotEqual(result.returncode, 0)
            self.assertIn('MILESTONE_AUTOMATION_STATUS: READY', result.stdout, result.stderr)
            self.assertIn('muse working...', result.stdout)
            self.assertIn('[muse] Model step started.', result.stdout)
            self.assertNotIn('Implementing M015', result.stdout)
            self.assertEqual(before, git('rev-parse', 'HEAD'))
            for flag in (
                'exec', '--model', '--reasoning-effort', '--workspace',
                '--json', '--trust-workspace', '--disable-approval',
                '--disable-sandbox',
            ):
                self.assertIn(flag, result.stdout)

    def test_muse_retries_a_transient_provider_failure(self):
        with self.fixture() as (root, env, git):
            count_file = root / 'muse-count'
            env.update({
                'MUSE_FIXTURE_MODE': 'transient-once',
                'MUSE_FIXTURE_COUNT_FILE': str(count_file),
                'MUSE_RUNNER_RETRY_DELAY_SECONDS': '0',
            })
            before = git('rev-parse', 'HEAD')

            result = self.run_fixture(root, env, '--muse')

            self.assertNotEqual(result.returncode, 0)
            self.assertEqual(count_file.read_text().strip(), '2')
            self.assertIn('Transient provider failure; retrying attempt 2/3', result.stdout)
            self.assertIn('MILESTONE_AUTOMATION_STATUS: READY', result.stdout)
            self.assertEqual(before, git('rev-parse', 'HEAD'))

    def test_muse_does_not_retry_a_permanent_failure(self):
        with self.fixture() as (root, env, git):
            count_file = root / 'muse-count'
            env.update({
                'MUSE_FIXTURE_MODE': 'permanent',
                'MUSE_FIXTURE_COUNT_FILE': str(count_file),
                'MUSE_RUNNER_RETRY_DELAY_SECONDS': '0',
            })

            result = self.run_fixture(root, env, '--muse')

            self.assertNotEqual(result.returncode, 0)
            self.assertEqual(count_file.read_text().strip(), '1')
            self.assertNotIn('Transient provider failure', result.stdout)

    def test_muse_validates_only_the_successful_retry_output(self):
        with self.fixture() as (root, env, git):
            count_file = root / 'muse-count'
            env.update({
                'MUSE_FIXTURE_MODE': 'stale-status',
                'MUSE_FIXTURE_COUNT_FILE': str(count_file),
                'MUSE_RUNNER_RETRY_DELAY_SECONDS': '0',
            })

            result = self.run_fixture(root, env, '--muse')

            self.assertNotEqual(result.returncode, 0)
            self.assertEqual(count_file.read_text().strip(), '2')
            self.assertIn('did not report the required status', result.stderr)

    def test_muse_current_planning_commit_is_reused(self):
        with self.fixture() as (root, env, git):
            self.prepare_plan(root, git)
            before = git('rev-parse', 'HEAD')
            result = self.run_fixture(root, env, '-muse')
            self.assertIn('Reusing it.', result.stdout, result.stderr)
            self.assertIn('Implementing M015', result.stdout)
            self.assertNotIn('Planning M015', result.stdout)
            # Fake implementation reports READY, not COMPLETE, so no commit follows.
            self.assertNotEqual(result.returncode, 0)
            self.assertEqual(before, git('rev-parse', 'HEAD'))

    def test_upstream_changes_are_pulled_before_commit(self):
        with self.fixture() as (root, env, git):
            self.prepare_plan(root, git)
            remote = git('remote', 'get-url', 'origin').strip()
            with tempfile.TemporaryDirectory() as peer_temp:
                peer = Path(peer_temp).resolve()
                subprocess.check_call(
                    ['git', 'clone', '--branch', 'main', remote, str(peer)],
                    stdout=subprocess.DEVNULL,
                    stderr=subprocess.DEVNULL,
                )
                subprocess.check_call(['git', '-C', str(peer), 'config', 'user.email', 'peer@example.invalid'])
                subprocess.check_call(['git', '-C', str(peer), 'config', 'user.name', 'Peer'])
                (peer / 'REMOTE.md').write_text('upstream change\n')
                subprocess.check_call(['git', '-C', str(peer), 'add', 'REMOTE.md'])
                subprocess.check_call(['git', '-C', str(peer), 'commit', '-m', 'Upstream change'], stdout=subprocess.DEVNULL)
                subprocess.check_call(['git', '-C', str(peer), 'push', 'origin', 'main'], stdout=subprocess.DEVNULL)

            env['CODEX_FIXTURE_MODE'] = 'complete'
            result = self.run_fixture(root, env)

            self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
            self.assertIn('Pulling upstream changes before commit...', result.stdout)
            self.assertEqual((root / 'REMOTE.md').read_text(), 'upstream change\n')
            self.assertIn('implementation', (root / 'README.md').read_text())
            self.assertEqual(git('log', '-1', '--format=%s').strip(), 'M015 implemented')


if __name__ == '__main__':
    unittest.main()
