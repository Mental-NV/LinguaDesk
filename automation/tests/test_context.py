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
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp).resolve()
            (root / 'automation').mkdir()
            for name in ('context.py', 'run-milestones.sh', 'plan.prompt.md', 'implement.prompt.md'):
                shutil.copyfile(MODULE.parent / name, root / 'automation' / name)
            (root / 'docs/08-backlogs').mkdir(parents=True)
            (root / 'docs/07-roadmap.md').write_text('| M015 | Test |\n')
            (root / 'README.md').write_text('# Test\n')
            (root / 'backend').mkdir()
            (root / 'backend/LinguaDesk.slnx').write_text('test')
            (root / 'fake-bin').mkdir()
            bodies = {
                'dotnet': 'exit 0',
                'codex': 'echo MILESTONE_AUTOMATION_STATUS: READY',
                'muse': 'echo \"muse-harness-args: $*\"\necho muse working...\necho MILESTONE_AUTOMATION_STATUS: READY',
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
            self.assertNotIn('Implementing M015', result.stdout)
            self.assertEqual(before, git('rev-parse', 'HEAD'))
            for flag in ('exec', '--model', '--reasoning-effort', '--workspace', '--trust-workspace', '--disable-approval'):
                self.assertIn(flag, result.stdout)

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


if __name__ == '__main__':
    unittest.main()
