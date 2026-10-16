import importlib.util
from pathlib import Path

spec = importlib.util.spec_from_file_location(
    "text_lint", Path(__file__).resolve().parents[2] / "tools/text_lint.py")
text_lint = importlib.util.module_from_spec(spec)
spec.loader.exec_module(text_lint)


def test_standard_texts_follow_language_rules():
    assert text_lint.lint() == []
