#!/usr/bin/env python3
"""Render Muse JSONL output as a plain, incrementally flushed transcript."""

import json
import os
from pathlib import Path
import re
import sys


RESET = '\033[0m'
COLORS = {
    'assistant': '\033[35m',
    'tool': '\033[36m',
    'error': '\033[31m',
}


def _single_line(value, limit=160):
    if not isinstance(value, str):
        return None
    value = ' '.join(value.split())
    if not value:
        return None
    if len(value) > limit:
        return value[:limit - 1] + '…'
    return value


def _display_path(value):
    value = _single_line(value, 140)
    if not value:
        return None
    if value.startswith('workspace:'):
        return value.removeprefix('workspace:')

    path = Path(value).expanduser()
    if not path.is_absolute():
        return value
    try:
        return str(path.relative_to(Path.cwd()))
    except ValueError:
        return str(path)


def _command(value):
    value = _single_line(value, 120)
    if not value:
        return None
    patterns = (
        (r'(?i)((?:api[_-]?key|token|password|secret)\s*=\s*)[^\s;]+', r'\1<redacted>'),
        (r'(?i)(--(?:api-key|token|password|secret)\s+)[^\s;]+', r'\1<redacted>'),
        (r'(?i)(authorization:\s*bearer\s+)[^\s;]+', r'\1<redacted>'),
    )
    for pattern, replacement in patterns:
        value = re.sub(pattern, replacement, value)
    return value


def _result_detail(tool_name, text, details):
    if tool_name == 'bash':
        return _command(details.get('command'))

    path = _display_path(details.get('path'))
    if path:
        return path
    if not isinstance(text, str):
        return None

    if tool_name == 'read_file':
        match = re.match(r'Read (?:text|binary) file `([^`]+)`\.', text)
        if match:
            return _display_path(match.group(1))
    elif tool_name == 'read_skill':
        match = re.match(r'<read-skill-result name="([^"]+)"', text)
        if match:
            return _single_line(match.group(1), 140)
    elif tool_name in {'write_file', 'apply_patch'}:
        match = re.match(r'(?:wrote|Wrote|Updated) (?:\d+ bytes (?:to )?)?`?([^`\n]+)`?', text)
        if match:
            return _display_path(match.group(1).rstrip('.'))
    return None


def _tool_result(payload):
    facts = payload.get('correlation_facts') or {}
    if not isinstance(facts, dict):
        facts = {}
    tool_name = _single_line(facts.get('tool_name')) or 'tool'
    outcome = _single_line(facts.get('outcome'))

    details = {}
    text = payload.get('text')
    if isinstance(text, str):
        try:
            candidate = json.loads(text)
        except json.JSONDecodeError:
            candidate = None
        if isinstance(candidate, dict):
            details = candidate

    detail = _result_detail(tool_name, text, details)
    status = _single_line(details.get('terminal_status')) or outcome
    exit_code = details.get('exit_code')
    subject = f'{tool_name}: {detail}' if detail else tool_name

    if status:
        suffix = status
        if isinstance(exit_code, int):
            suffix += f' (exit {exit_code})'
        return f'{subject} — {suffix}'
    return f'{subject} finished'


def _tool_action(tool_name):
    actions = {
        'apply_patch': 'Applying patch…',
        'bash': 'Running command…',
        'read_file': 'Reading file…',
        'read_skill': 'Loading skill…',
        'write_file': 'Writing file…',
    }
    return actions.get(tool_name, f'Running {tool_name}…')


def render(lines, output, color=False):
    saw_delta = False
    ends_with_newline = True
    model_steps = 0
    tool_results_since_model = 0

    def write_text(text):
        nonlocal ends_with_newline
        output.write(text)
        output.flush()
        ends_with_newline = text.endswith('\n')

    def write_progress(message, role='tool'):
        nonlocal ends_with_newline
        if not ends_with_newline:
            output.write('\n')
        line = f'[muse:{role}] {message}'
        if color:
            line = f'{COLORS[role]}{line}{RESET}'
        output.write(line + '\n')
        output.flush()
        ends_with_newline = True

    for line in lines:
        try:
            event = json.loads(line)
        except (json.JSONDecodeError, TypeError):
            write_text(line)
            continue

        if not isinstance(event, dict):
            write_text(line)
            continue

        payload = event.get('payload') or {}
        if not isinstance(payload, dict):
            continue
        payload_type = event.get('payload_type')

        if payload_type == 'run.output.delta':
            text = payload.get('text')
            if isinstance(text, str) and text:
                if not saw_delta:
                    write_progress('Response:', 'assistant')
                write_text(text)
                saw_delta = True
        elif payload_type == 'run.terminal.completed' and not saw_delta:
            text = payload.get('text')
            if isinstance(text, str) and text:
                write_progress('Response:', 'assistant')
                write_text(text)
        elif payload_type == 'task.lifecycle.proposed':
            lifecycle = payload.get('event') or {}
            if not isinstance(lifecycle, dict):
                continue
            task_kind = lifecycle.get('task_kind')
            if not isinstance(task_kind, str):
                continue
            if task_kind.startswith('model.'):
                if model_steps == 0:
                    message = 'Analyzing request…'
                elif tool_results_since_model == 1:
                    message = 'Reviewing 1 tool result…'
                elif tool_results_since_model > 1:
                    message = f'Reviewing {tool_results_since_model} tool results…'
                else:
                    message = 'Continuing analysis…'
                write_progress(message, 'assistant')
                model_steps += 1
                tool_results_since_model = 0
            elif task_kind.startswith('tool.'):
                tool_name = _single_line(task_kind.removeprefix('tool.'))
                if tool_name:
                    write_progress(_tool_action(tool_name))
        elif payload_type == 'tool.result':
            tool_results_since_model += 1
            write_progress(_tool_result(payload))
        elif payload_type == 'task.lifecycle.status':
            lifecycle = payload.get('event') or {}
            if not isinstance(lifecycle, dict):
                continue
            details = lifecycle.get('details') or {}
            if not isinstance(details, dict):
                details = {}
            if details.get('phase') == 'stream_failed':
                message = _single_line(lifecycle.get('message'))
                write_progress(message or 'Provider stream failed.', 'error')
        elif payload_type == 'task.lifecycle.failed':
            lifecycle = payload.get('event') or {}
            if isinstance(lifecycle, dict):
                reason = _single_line(lifecycle.get('reason'))
                if reason:
                    write_progress(f'Task failed: {reason}', 'error')
        elif payload_type == 'run.terminal.failed':
            reason = payload.get('reason')
            if isinstance(reason, str) and reason:
                write_progress(f'Run failed: {_single_line(reason)}', 'error')

    if not ends_with_newline:
        write_text('\n')


if __name__ == '__main__':
    use_color = (
        '--color' in sys.argv[1:]
        and 'NO_COLOR' not in os.environ
        and os.environ.get('TERM') != 'dumb'
    )
    render(sys.stdin, sys.stdout, color=use_color)
