"""Flushed, timestamp-free progress using code-owned labels, never command output."""
from contextlib import contextmanager
import threading
import time


def log(message):
    print('[deploy] ' + message, flush=True)


@contextmanager
def stage(label, heartbeat_seconds=20):
    started = time.monotonic()
    finished = threading.Event()
    log('Starting: ' + label + '.')

    def heartbeat():
        while not finished.wait(heartbeat_seconds):
            log(f'Waiting: {label} ({int(time.monotonic() - started)}s elapsed).')

    worker = threading.Thread(target=heartbeat, daemon=True)
    worker.start()
    outcome = 'Failed'
    try:
        yield
        outcome = 'Completed'
    finally:
        finished.set()
        worker.join()
        log(f'{outcome}: {label} ({int(time.monotonic() - started)}s elapsed).')
