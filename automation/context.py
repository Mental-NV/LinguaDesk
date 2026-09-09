#!/usr/bin/env python3
"""Bounded, deterministic documentation context. Python standard library only."""
import argparse
import hashlib
import json
from pathlib import Path
import re
import subprocess
import sys
from urllib.parse import unquote

ROOT = Path(__file__).resolve().parents[1]
RULES = ('docs/00-SDD-Planning-Workflow.md', 'docs/00-workflow/execution.md')
SCOPE = re.compile(r'[MF][0-9]{3,}')


def path_for(value):
    path = (ROOT / value).resolve()
    if not path.is_relative_to(ROOT) or not path.is_file():
        raise ValueError(f'Missing or out-of-repository file: {value}')
    return path


def digest(text):
    return hashlib.sha256(text.encode('utf-8')).hexdigest()


def unfenced(text):
    return re.sub(r'^(`{3,}|~{3,}).*?^\1\s*$', '', text, flags=re.M | re.S)


def headings(text):
    """Return heading offsets without interpreting fenced examples as headings."""
    result, offset, fence = [], 0, None
    for line in text.splitlines(keepends=True):
        marker = re.match(r'^\s*(`{3,}|~{3,})', line)
        if marker:
            if fence is None:
                fence = marker[1][0]
            elif marker[1][0] == fence:
                fence = None
        elif fence is None:
            match = re.match(r'^(#{1,6}) (.+?)\s*$', line)
            if match:
                result.append((len(match[1]), match[2], offset))
        offset += len(line)
    return result


def excerpt(source):
    text = path_for(source['path']).read_text()
    if 'heading' in source:
        hs = headings(text)
        matches = [i for i, (_, h, _) in enumerate(hs) if h == source['heading']]
        if len(matches) != 1:
            raise ValueError(f"Expected one heading {source['heading']!r} in {source['path']}")
        i = matches[0]
        level, _, start = hs[i]
        end = next((pos for lev, _, pos in hs[i + 1:] if lev <= level), len(text))
        return text[start:end].strip() + '\n'
    if 'ids' in source:
        selected = []
        lines = text.splitlines()
        for item in source['ids']:
            matches = [line for line in lines if re.match(
                r'^\|\s*' + re.escape(item) + r'(?=\s*(?:\||/|—))', line)]
            if len(matches) != 1:
                raise ValueError(f"Expected one table row for {item} in {source['path']}; select its containing heading for grouped IDs")
            selected.append(matches[0])
        return '\n'.join(selected) + '\n'
    return text


def package(scope):
    if not SCOPE.fullmatch(scope):
        raise ValueError('Use a stable scope ID such as M015 or F001')
    return ROOT / 'docs' / '08-backlogs' / scope


def manifest(scope):
    directory = package(scope)
    for name in ('backlog.md', 'spec.md', 'plan.md', 'tasks.md', 'context.json'):
        path_for(str((directory / name).relative_to(ROOT)))
    data = json.loads((directory / 'context.json').read_text())
    if not isinstance(data, dict) or data.get('version') != 1 or data.get('scope') != scope:
        raise ValueError('context.json needs version: 1 and the matching scope ID')
    sources = data.get('sources')
    if not isinstance(sources, list) or not sources:
        raise ValueError('context.json needs a nonempty sources array')
    seen = set()
    for source in sources:
        if not isinstance(source, dict) or not isinstance(source.get('path'), str):
            raise ValueError('Each source needs a repository-relative path')
        if Path(source['path']).is_absolute() or '..' in Path(source['path']).parts:
            raise ValueError('Source paths must be normalized repository-relative paths')
        if source.get('use') not in ('excerpt', 'reference') or not isinstance(source.get('reason'), str) or not source['reason'].strip():
            raise ValueError('Each source needs use: excerpt/reference and a nonempty reason')
        if 'heading' in source and 'ids' in source:
            raise ValueError('Select a heading OR ID rows in each source entry')
        if 'heading' in source and (not isinstance(source['heading'], str) or not source['heading']):
            raise ValueError('heading must be a nonempty exact heading title')
        if 'ids' in source and (not isinstance(source['ids'], list) or not source['ids'] or
                                any(not isinstance(i, str) or not i for i in source['ids']) or
                                len(set(source['ids'])) != len(source['ids'])):
            raise ValueError('ids must be a nonempty array of unique ID strings')
        key = json.dumps({k: source[k] for k in ('path', 'heading', 'ids') if k in source}, sort_keys=True)
        if key in seen:
            raise ValueError('Duplicate source selection: ' + source['path'])
        seen.add(key)
        path_for(source['path'])
    return directory, data


def sealed_sources(data):
    return [{k: v for k, v in source.items() if k != 'sha256'} for source in data['sources']]


def lock(scope):
    directory, data = manifest(scope)
    for source in data['sources']:
        source['sha256'] = digest(excerpt(source))
    data['source_map_sha256'] = digest(json.dumps(sealed_sources(data), sort_keys=True))
    data['baseline_commit'] = subprocess.check_output(
        ['git', '-C', str(ROOT), 'rev-parse', 'HEAD'], text=True).strip()
    paths = [*RULES, *(str((directory / n).relative_to(ROOT)) for n in ('spec.md', 'plan.md'))]
    data['locked_files'] = {p: digest(path_for(p).read_text()) for p in paths}
    (directory / 'context.json').write_text(json.dumps(data, indent=2, ensure_ascii=False) + '\n')
    print(f'{scope}: locked {len(data["sources"])} source selections; semantic readiness still requires review.')


def check(scope):
    directory, data = manifest(scope)
    failures = []
    if not isinstance(data.get('locked_files', {}), dict):
        raise ValueError('locked_files must be an object')
    if not isinstance(data.get('baseline_commit', ''), str) or not re.fullmatch(r'[0-9a-f]{40,64}', data.get('baseline_commit', '')):
        failures.append('missing baseline commit')
    if data.get('source_map_sha256') != digest(json.dumps(sealed_sources(data), sort_keys=True)):
        failures.append('source map changed or has not been locked')
    required = [*RULES, *(str((directory / n).relative_to(ROOT)) for n in ('spec.md', 'plan.md'))]
    for path in required:
        if data.get('locked_files', {}).get(path) != digest(path_for(path).read_text()):
            failures.append('changed/unlocked ' + path)
    for source in data['sources']:
        if source.get('sha256') != digest(excerpt(source)):
            failures.append('changed/unlocked source ' + source['path'] + ' ' +
                            str(source.get('heading', source.get('ids', '(whole file)'))))
    if failures:
        raise ValueError('; '.join(failures) + '. Review affected inputs and plan before re-locking.')
    return directory, data


def block(path, text, selector=''):
    return f'\n--- {path}{(" :: " + selector) if selector else ""} ---\n\n{text.rstrip()}\n'


def packet(scope):
    directory, data = check(scope)
    parts = [block(p, path_for(p).read_text()) for p in RULES]
    # Sort stable reference material independently of author insertion order.
    sources = sorted(data['sources'], key=lambda s: (s['path'], s.get('heading', ''), ','.join(s.get('ids', []))))
    for source in sources:
        if source['use'] == 'excerpt':
            selector = source.get('heading', ', '.join(source.get('ids', [])))
            parts.append(block(source['path'], excerpt(source), selector))
    parts.append(f'\nSelected scope: {scope}. Baseline commit: {data["baseline_commit"]}.\n')
    parts.append('\nOn-demand references (open only for the stated reason):\n')
    for source in sources:
        if source['use'] == 'reference':
            parts.append(f'- {source["path"]} :: {source.get("heading", source.get("ids", "whole file"))}: {source["reason"]}\n')
    for name in ('spec.md', 'plan.md', 'backlog.md', 'tasks.md'):
        file = directory / name
        parts.append(block(str(file.relative_to(ROOT)), file.read_text()))
    return ''.join(parts)


def anchor(title):
    title = re.sub(r'\[([^]]+)\]\([^)]+\)', r'\1', title)
    return re.sub(r'[^\w\- ]', '', title.lower()).replace(' ', '-')


def document_paths():
    return [ROOT / 'README.md', *sorted((ROOT / 'docs').rglob('*.md')), *sorted((ROOT / 'automation').glob('*.md'))]


def audit():
    errors = []
    for path in document_paths():
        text = unfenced(path.read_text())
        for match in re.finditer(r'(?<!!)\[[^\]\n]*\]\(([^)\n]+)\)', text):
            url = match[1]
            if re.match(r'[a-zA-Z][\w+.-]*:', url) or url.startswith('//'):
                continue
            target, _, fragment = unquote(url).partition('#')
            target_path = (path.parent / target).resolve() if target else path
            if not target_path.exists():
                errors.append(f'{path.relative_to(ROOT)}: missing {url}')
            elif fragment and target_path.suffix == '.md':
                headings_set = {anchor(h) for _, h, _ in headings(target_path.read_text())}
                explicit = set(re.findall(r'<a\s+(?:id|name)=["\']([^"\']+)', target_path.read_text()))
                if fragment not in headings_set | explicit:
                    errors.append(f'{path.relative_to(ROOT)}: missing anchor {url}')
        if re.search(r'(?:docs/)?08-backlogs/[MF]\d+-[^\s)`]+\.md|specs/\d+-', text):
            errors.append(f'{path.relative_to(ROOT)}: legacy package path')
    for directory in sorted((ROOT / 'docs' / '08-backlogs').iterdir()):
        if not directory.is_dir() or not SCOPE.fullmatch(directory.name):
            errors.append(f'Invalid package folder: {directory.relative_to(ROOT)}')
            continue
        for name in ('backlog.md', 'spec.md', 'plan.md', 'tasks.md'):
            if not (directory / name).is_file():
                errors.append(f'Missing {directory.name}/{name}')
    if errors:
        raise ValueError('\n'.join(errors))
    print(f'Checked local links/anchors and package layout in {len(document_paths())} Markdown documents.')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    sub = parser.add_subparsers(dest='command', required=True)
    for command in ('outline', 'read'):
        p = sub.add_parser(command)
        p.add_argument('path')
        if command == 'read':
            group = p.add_mutually_exclusive_group()
            group.add_argument('--heading')
            group.add_argument('--ids', nargs='+')
    for command in ('lock', 'check', 'packet', 'measure'):
        sub.add_parser(command).add_argument('scope')
    sub.add_parser('audit')
    args = parser.parse_args()
    try:
        if args.command == 'outline':
            for level, title, _ in headings(path_for(args.path).read_text()):
                print('#' * level + ' ' + title)
        elif args.command == 'read':
            source = {k: v for k, v in vars(args).items() if k in ('path', 'heading', 'ids') and v is not None}
            print(excerpt(source), end='')
        elif args.command == 'lock':
            lock(args.scope)
        elif args.command == 'check':
            _, data = check(args.scope)
            print(f'{args.scope}: selected inputs and spec/plan unchanged ({len(data["sources"])} sources).')
        elif args.command == 'packet':
            print(packet(args.scope), end='')
        elif args.command == 'measure':
            text = packet(args.scope)
            print(json.dumps({'scope': args.scope, 'words': len(text.split()), 'utf8_bytes': len(text.encode()),
                              'rough_tokens_bytes_div_4': (len(text.encode()) + 3) // 4,
                              'note': 'Size estimate only; actual tokenizer, tool context and cache usage differ.'}, indent=2))
        elif args.command == 'audit':
            audit()
    except (ValueError, OSError, KeyError, TypeError, subprocess.CalledProcessError) as error:
        print(str(error), file=sys.stderr)
        return 1
    return 0


if __name__ == '__main__':
    sys.exit(main())
