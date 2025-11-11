import argparse
import re
from dataclasses import dataclass
from pathlib import Path
from typing import Callable, Iterable


def _consume_string(text: str, index: int, delim: str, raw_hashes: int) -> int:
    n = len(text)
    i = index
    if delim == '"' and raw_hashes > 0:
        while i < n:
            ch = text[i]
            if ch == '"' and text[i + 1 : i + 1 + raw_hashes] == '#' * raw_hashes:
                return i + 1 + raw_hashes
            i += 1
        return n
    escape = False
    while i < n:
        ch = text[i]
        if escape:
            escape = False
        elif ch == '\\':
            escape = True
        elif ch == delim:
            return i + 1
        i += 1
    return n


def _scan_raw_string(text: str, index: int) -> tuple[int, int] | None:
    n = len(text)
    i = index + 1
    hashes = 0
    while i < n and text[i] == '#':
        hashes += 1
        i += 1
    if i < n and text[i] == '"':
        return i + 1, hashes
    return None


def _consume_comment(text: str, index: int) -> int:
    n = len(text)
    i = index + 2
    while i < n - 1:
        if text[i] == '*' and text[i + 1] == '/':
            return i + 2
        i += 1
    return n


_FN_NAME_RE = re.compile(r"\bfn\s+([A-Za-z0-9_]+)")


@dataclass
class _Function:
    fn_index: int
    body_start: int
    body_end: int
    fn_name: str


def _find_next_function(text: str, start: int) -> _Function | None:
    n = len(text)
    i = start
    in_line_comment = False
    in_block_comment = False
    in_string = False
    string_delim = ''
    raw_hashes = 0

    while i < n:
        ch = text[i]
        next_ch = text[i + 1] if i + 1 < n else ''

        if in_line_comment:
            if ch == '\n':
                in_line_comment = False
            i += 1
            continue

        if in_block_comment:
            if ch == '*' and next_ch == '/':
                in_block_comment = False
                i += 2
            else:
                i += 1
            continue

        if in_string:
            i = _consume_string(text, i, string_delim, raw_hashes)
            in_string = False
            string_delim = ''
            raw_hashes = 0
            continue

        if ch == '/' and next_ch == '/':
            in_line_comment = True
            i += 2
            continue
        if ch == '/' and next_ch == '*':
            in_block_comment = True
            i += 2
            continue
        if ch == '"':
            in_string = True
            string_delim = '"'
            raw_hashes = 0
            i += 1
            continue
        if ch == '\'':
            in_string = True
            string_delim = '\''
            raw_hashes = 0
            i += 1
            continue
        if ch == 'r':
            raw = _scan_raw_string(text, i)
            if raw is not None:
                i, raw_hashes = raw
                in_string = True
                string_delim = '"'
                continue

        if text.startswith('fn', i):
            prev = text[i - 1] if i > 0 else ''
            if prev.isalnum() or prev == '_':
                i += 2
                continue
            fn_info = _parse_function(text, i)
            if fn_info is not None:
                return fn_info
        i += 1

    return None


def _parse_function(text: str, fn_index: int) -> _Function | None:
    n = len(text)
    j = fn_index + 2
    in_line_comment = False
    in_block_comment = False
    in_string = False
    string_delim = ''
    raw_hashes = 0
    paren_depth = 0
    angle_depth = 0
    bracket_depth = 0
    header_end = fn_index

    while j < n:
        ch = text[j]
        next_ch = text[j + 1] if j + 1 < n else ''

        if in_line_comment:
            if ch == '\n':
                in_line_comment = False
            j += 1
            continue

        if in_block_comment:
            if ch == '*' and next_ch == '/':
                in_block_comment = False
                j += 2
            else:
                j += 1
            continue

        if in_string:
            j = _consume_string(text, j, string_delim, raw_hashes)
            in_string = False
            string_delim = ''
            raw_hashes = 0
            continue

        if ch == '/' and next_ch == '/':
            in_line_comment = True
            j += 2
            continue
        if ch == '/' and next_ch == '*':
            in_block_comment = True
            j = _consume_comment(text, j)
            continue
        if ch == '"':
            in_string = True
            string_delim = '"'
            raw_hashes = 0
            j += 1
            continue
        if ch == '\'':
            in_string = True
            string_delim = '\''
            raw_hashes = 0
            j += 1
            continue
        if ch == 'r':
            raw = _scan_raw_string(text, j)
            if raw is not None:
                j, raw_hashes = raw
                in_string = True
                string_delim = '"'
                continue

        if ch == '(':
            paren_depth += 1
        elif ch == ')':
            paren_depth = max(paren_depth - 1, 0)
        elif ch == '[':
            bracket_depth += 1
        elif ch == ']':
            bracket_depth = max(bracket_depth - 1, 0)
        elif ch == '<':
            angle_depth += 1
        elif ch == '>':
            angle_depth = max(angle_depth - 1, 0)
        elif ch == ';' and paren_depth == 0 and angle_depth == 0 and bracket_depth == 0:
            return None
        elif ch == '{' and paren_depth == 0 and angle_depth == 0 and bracket_depth == 0:
            body_start = j
            header_end = j
            j += 1
            break

        j += 1
    else:
        return None

    depth = 1
    in_line_comment = False
    in_block_comment = False
    in_string = False
    string_delim = ''
    raw_hashes = 0
    body_cursor = j

    while body_cursor < n:
        ch = text[body_cursor]
        next_ch = text[body_cursor + 1] if body_cursor + 1 < n else ''

        if in_line_comment:
            if ch == '\n':
                in_line_comment = False
            body_cursor += 1
            continue

        if in_block_comment:
            if ch == '*' and next_ch == '/':
                in_block_comment = False
                body_cursor += 2
            else:
                body_cursor += 1
            continue

        if in_string:
            body_cursor = _consume_string(text, body_cursor, string_delim, raw_hashes)
            in_string = False
            string_delim = ''
            raw_hashes = 0
            continue

        if ch == '/' and next_ch == '/':
            in_line_comment = True
            body_cursor += 2
            continue
        if ch == '/' and next_ch == '*':
            in_block_comment = True
            body_cursor = _consume_comment(text, body_cursor)
            continue
        if ch == '"':
            in_string = True
            string_delim = '"'
            raw_hashes = 0
            body_cursor += 1
            continue
        if ch == '\'':
            in_string = True
            string_delim = '\''
            raw_hashes = 0
            body_cursor += 1
            continue
        if ch == 'r':
            raw = _scan_raw_string(text, body_cursor)
            if raw is not None:
                body_cursor, raw_hashes = raw
                in_string = True
                string_delim = '"'
                continue

        if ch == '{':
            depth += 1
        elif ch == '}':
            depth -= 1
            if depth == 0:
                header_slice = text[fn_index:header_end]
                match = _FN_NAME_RE.search(header_slice)
                fn_name = match.group(1) if match else '<unknown>'
                return _Function(fn_index, body_start, body_cursor, fn_name)
        body_cursor += 1

    return None


def _replace_bodies(text: str, builder: Callable[[str, str, str], str]) -> str:
    functions: list[_Function] = []
    search_index = 0
    while True:
        fn_info = _find_next_function(text, search_index)
        if fn_info is None:
            break
        functions.append(fn_info)
        search_index = fn_info.body_end + 1

    if not functions:
        return text

    new_text = text
    for fn_info in reversed(functions):
        body_start = fn_info.body_start
        body_end = fn_info.body_end

        line_start = new_text.rfind('\n', 0, body_start)
        line_start = 0 if line_start == -1 else line_start + 1
        indent = ''
        cursor = line_start
        while cursor < body_start and new_text[cursor] in (' ', '\t'):
            indent += new_text[cursor]
            cursor += 1

        body_indent = indent + '    '
        stub_body = builder(indent, body_indent, fn_info.fn_name)

        new_text = new_text[: body_start + 1] + stub_body + new_text[body_end:]

    return new_text


def _todo_body(indent: str, body_indent: str, fn_name: str) -> str:
    return (
        '\n'
        f"{body_indent}// TODO: port `{fn_name}` from xi-editor upstream.\n"
        f"{body_indent}todo!(\"stub\");\n"
        f"{indent}"
    )


def _doc_body(indent: str, body_indent: str, _fn_name: str) -> str:
    return f"\n{body_indent}...\n{indent}"


def _gather_files(paths: Iterable[Path]) -> list[Path]:
    files: set[Path] = set()
    for path in paths:
        if path.is_dir():
            for child in path.rglob('*.rs'):
                files.add(child)
        elif path.is_file() and path.suffix == '.rs':
            files.add(path)
    return sorted(files, key=lambda p: p.resolve().as_posix())


def _write_markdown(output_path: Path, entries: list[tuple[str, str]]) -> None:
    output_path.parent.mkdir(parents=True, exist_ok=True)
    with output_path.open('a', encoding='utf-8') as handle:
        for rel_path, content in entries:
            handle.write(f'## {rel_path}\n\n')
            handle.write('```rust\n')
            handle.write(content.rstrip())
            handle.write('\n```\n\n')


def main() -> None:
    parser = argparse.ArgumentParser(description='Stub Rust function bodies or export skeleton markdown.')
    parser.add_argument('paths', nargs='+', type=Path, help='Rust source files or directories to process')
    parser.add_argument('--mode', choices=['stub', 'doc'], default='doc', help='Whether to write stubs back to files or emit markdown (default: doc).')
    parser.add_argument('--output', type=Path, default=Path('docs/reference/rust-skeleton.md'), help='Markdown output path (doc mode only).')
    parser.add_argument('--root', type=Path, default='.', help='Base directory for relative paths in markdown (defaults to current working directory).')
    parser.add_argument('--truncate-output', action='store_true', help='Truncate the output file before appending (doc mode only).')
    args = parser.parse_args()

    files = _gather_files(args.paths)
    if not files:
        print('No Rust files found for processing.')
        return

    if args.mode == 'stub':
        for file_path in files:
            original = file_path.read_text(encoding='utf-8')
            transformed = _replace_bodies(original, _todo_body)
            if transformed != original:
                file_path.write_text(transformed, encoding='utf-8')
        return

    root = (args.root or Path.cwd()).resolve()
    if args.truncate_output:
        args.output.parent.mkdir(parents=True, exist_ok=True)
        args.output.write_text('', encoding='utf-8')

    entries: list[tuple[str, str]] = []
    for file_path in files:
        original = file_path.read_text(encoding='utf-8')
        skeleton = _replace_bodies(original, _doc_body)
        try:
            rel_path = file_path.resolve().relative_to(root)
            rel_text = rel_path.as_posix()
        except ValueError:
            rel_text = file_path.resolve().as_posix()
        entries.append((rel_text, skeleton))

    _write_markdown(args.output, entries)


if __name__ == '__main__':
    main()