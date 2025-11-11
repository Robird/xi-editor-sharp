## ref-outline/rust/unicode/src/emoji.rs

```rust
#[rustfmt::skip]
pub const EMOJI_TABLE: [char; 1250] = [...];

#[rustfmt::skip]
pub const EMOJI_MODIFIER_BASE_TABLE: [char; 106] = [...];
```

## ref-outline/rust/unicode/src/lib.rs

```rust
#![no_std]

extern crate alloc;

mod emoji;
mod tables;

use core::cmp::Ordering;

use crate::emoji::*;
use crate::tables::*;

/// The Unicode line breaking property of the given code point.
///
/// This is given as a numeric value which matches the ULineBreak
/// enum value from ICU.
pub fn linebreak_property(cp: char) -> u8 {...}

/// The Unicode line breaking property of the given code point.
///
/// Look up the line breaking property for the first code point in the
/// string. Return the property as a numeric value, and also the utf-8
/// length of the codepoint, for convenience.
pub fn linebreak_property_str(s: &str, ix: usize) -> (u8, usize) {...}

/// An iterator which produces line breaks according to the UAX 14 line
/// breaking algorithm. For each break, return a tuple consisting of the offset
/// within the source string and a bool indicating whether it's a hard break.
///
/// There is never a break at the beginning of the string (thus, the empty string
/// produces no breaks). For non-empty strings, there is always a break at the
/// end. It is indicated as a hard break when the string is terminated with a
/// newline or other Unicode explicit line-end character.
#[derive(Copy, Clone)]
pub struct LineBreakIterator<'a> {
    s: &'a str,
    ix: usize,
    state: u8,
}

impl<'a> Iterator for LineBreakIterator<'a> {
    type Item = (usize, bool);

    // return break pos and whether it's a hard break
    fn next(&mut self) -> Option<(usize, bool)> {...}
}

impl<'a> LineBreakIterator<'a> {
    /// Create a new iterator for the given string slice.
    pub fn new(s: &str) -> LineBreakIterator {...}
}

/// A struct useful for computing line breaks in a rope or other non-contiguous
/// string representation. This is a trickier problem than iterating in a string
/// for a few reasons, the trickiest of which is that in the general case,
/// line breaks require an indeterminate amount of look-behind.
///
/// This is something of an "expert-level" interface, and should only be used if
/// the caller is prepared to respect all the invariants. Otherwise, you might
/// get inconsistent breaks depending on start position and leaf boundaries.
#[derive(Copy, Clone)]
pub struct LineBreakLeafIter {
    ix: usize,
    state: u8,
}

#[allow(clippy::derivable_impls)]
impl Default for LineBreakLeafIter {
    // A default value. No guarantees on what happens when next() is called
    // on this. Intended to be useful for empty ropes.
    fn default() -> LineBreakLeafIter {...}
}

impl LineBreakLeafIter {
    /// Create a new line break iterator suitable for leaves in a rope.
    /// Precondition: ix is at a code point boundary within s.
    pub fn new(s: &str, ix: usize) -> LineBreakLeafIter {...}

    /// Return break pos and whether it's a hard break. Note: hard break
    /// indication may go away, this may not be useful in actual application.
    /// If end of leaf is found, return leaf's len. This does not indicate
    /// a break, as that requires at least one more codepoint of context.
    /// If it is a break, then subsequent next call will return an offset of 0.
    /// EOT is always a break, so in the EOT case it's up to the caller
    /// to figure that out.
    ///
    /// For consistent results, always supply same `s` until end of leaf is
    /// reached (and initially this should be the same as in the `new` call).
    pub fn next(&mut self, s: &str) -> (usize, bool) {...}
}

fn is_in_asc_list<T: core::cmp::PartialOrd>(c: T, list: &[T], start: usize, end: usize) -> bool {...}

pub fn is_variation_selector(c: char) -> bool {...}

#[allow(clippy::wrong_self_convention)] // clippy wants &self for all of these
pub trait EmojiExt {
    fn is_regional_indicator_symbol(self) -> bool;
    fn is_emoji_modifier(self) -> bool;
    fn is_emoji_combining_enclosing_keycap(self) -> bool;
    fn is_emoji(self) -> bool;
    fn is_emoji_modifier_base(self) -> bool;
    fn is_tag_spec_char(self) -> bool;
    fn is_emoji_cancel_tag(self) -> bool;
    fn is_zwj(self) -> bool;
}

impl EmojiExt for char {
    fn is_regional_indicator_symbol(self) -> bool {...}
    fn is_emoji_modifier(self) -> bool {...}
    fn is_emoji_combining_enclosing_keycap(self) -> bool {...}
    fn is_emoji(self) -> bool {...}
    fn is_emoji_modifier_base(self) -> bool {...}
    fn is_tag_spec_char(self) -> bool {...}
    fn is_emoji_cancel_tag(self) -> bool {...}
    fn is_zwj(self) -> bool {...}
}

pub fn is_keycap_base(c: char) -> bool {...}
```

## ref-outline/rust/unicode/src/tables.rs

```rust
#[rustfmt::skip]
pub const LINEBREAK_1_2: [u8; 2048] = [...];

#[rustfmt::skip]
pub const LINEBREAK_3_ROOT: [u8; 1024] = [...];

#[rustfmt::skip]
pub const LINEBREAK_3_CHILD: [u8; 11712] = [...];

#[rustfmt::skip]
pub const LINEBREAK_4_ROOT: [u8; 272] = [...];

#[rustfmt::skip]
pub const LINEBREAK_4_MID: [u8; 960] = [...];

#[rustfmt::skip]
pub const LINEBREAK_4_LEAVES: [u8; 9408] = [...];
// 51 unique states
pub const N_LINEBREAK_CATEGORIES: usize = 43;

#[rustfmt::skip]
pub const LINEBREAK_STATE_MACHINE: [u8; 3827] = [...];
```

