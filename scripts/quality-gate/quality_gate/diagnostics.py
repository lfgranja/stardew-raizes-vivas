"""GitHub Actions annotations and configuration errors."""

from __future__ import annotations

from dataclasses import dataclass
from typing import Optional


class GateConfigurationError(Exception):
    """Raised when an input file is missing or malformed."""


@dataclass(frozen=True)
class Diagnostic:
    title: str
    message: str
    file: Optional[str] = None
    line: Optional[int] = None

    def to_github(self) -> str:
        if self.file is not None and self.line is not None:
            return f"::error file={self.file},line={self.line}::{self.message}"
        if self.file is not None:
            return f"::error file={self.file}::{self.message}"
        return f"::error title={self.title}::{self.message}"