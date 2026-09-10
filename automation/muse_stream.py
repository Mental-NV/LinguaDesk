#!/usr/bin/env python3
"""Render Muse JSONL output as a plain, incrementally flushed transcript."""

import json
import sys


def _single_line(value, limit=160):
    if not isinstance(value, str):
        return None
    value = ' '.join(value.split())
    if not value:
        return None
    if len(value) > limit:
        return value[:limit - 1] + '…'
    return value


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

    description = _single_line(details.get('description'))
    status = _single_line(details.get('terminal_status')) or outcome
    exit_code = details.get('exit_code')
    subject = description or tool_name

    if status:
        suffix = status
        if isinstance(exit_code, int):
            suffix += f' (exit {exit_code})'
        return f'{subject} — {suffix}'
    return f'{subject} finished'


def render(lines, output):
    saw_delta = False
    ends_with_newline = True

    def write_text(text):
        nonlocal ends_with_newline
        output.write(text)
        output.flush()
        ends_with_newline = text.endswith('\n')

    def write_progress(message):
        nonlocal ends_with_newline
        if not ends_with_newline:
            output.write('\n')
        output.write(f'[muse] {message}\n')
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
                write_text(text)
                saw_delta = True
        elif payload_type == 'run.terminal.completed' and not saw_delta:
            text = payload.get('text')
            if isinstance(text, str) and text:
                write_text(text)
        elif payload_type == 'task.lifecycle.proposed':
            lifecycle = payload.get('event') or {}
            if not isinstance(lifecycle, dict):
                continue
            task_kind = lifecycle.get('task_kind')
            if not isinstance(task_kind, str):
                continue
            if task_kind.startswith('model.'):
                write_progress('Model step started.')
            elif task_kind.startswith('tool.'):
                tool_name = _single_line(task_kind.removeprefix('tool.'))
                if tool_name:
                    write_progress(f'Running {tool_name}...')
        elif payload_type == 'tool.result':
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
                write_progress(message or 'Provider stream failed.')
        elif payload_type == 'task.lifecycle.failed':
            lifecycle = payload.get('event') or {}
            if isinstance(lifecycle, dict):
                reason = _single_line(lifecycle.get('reason'))
                if reason:
                    write_progress(f'Task failed: {reason}')
        elif payload_type == 'run.terminal.failed':
            reason = payload.get('reason')
            if isinstance(reason, str) and reason:
                write_progress(f'Run failed: {_single_line(reason)}')

    if not ends_with_newline:
        write_text('\n')


if __name__ == '__main__':
    render(sys.stdin, sys.stdout)
