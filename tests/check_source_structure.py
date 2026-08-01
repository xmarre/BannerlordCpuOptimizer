#!/usr/bin/env python3
"""Lightweight structural checks usable when a C# compiler is unavailable."""
from __future__ import annotations

import pathlib

ROOT = pathlib.Path(__file__).resolve().parents[1]


def strip_literals(text: str) -> str:
    out = []
    i = 0
    while i < len(text):
        if text.startswith("//", i):
            end = text.find("\n", i)
            i = len(text) if end < 0 else end
        elif text.startswith("/*", i):
            end = text.find("*/", i + 2)
            i = len(text) if end < 0 else end + 2
        elif text[i] in ('"', "'"):
            quote = text[i]
            i += 1
            while i < len(text):
                if text[i] == "\\":
                    i += 2
                elif text[i] == quote:
                    i += 1
                    break
                else:
                    i += 1
            out.append(" ")
        else:
            out.append(text[i])
            i += 1
    return "".join(out)


def main() -> int:
    files = list((ROOT / "src").rglob("*.cs"))
    assert files, "No C# files found"
    for path in files:
        text = strip_literals(path.read_text(encoding="utf-8"))
        pairs = {"{": "}", "(": ")", "[": "]"}
        stack = []
        for char in text:
            if char in pairs:
                stack.append((char, pairs[char]))
            elif char in pairs.values():
                assert stack and stack[-1][1] == char, f"Unbalanced {char} in {path}"
                stack.pop()
        assert not stack, f"Unclosed delimiter in {path}: {stack[-1]}"
    print(f"Structural checks passed for {len(files)} C# files.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
