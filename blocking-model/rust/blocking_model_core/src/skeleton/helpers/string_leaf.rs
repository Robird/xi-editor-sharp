const ASCII_NEWLINE: u8 = b'\n';

pub const MIN_LEAF: usize = 511;
pub const MAX_LEAF: usize = 1024;
pub const NEWLINE_WINDOW: usize = MAX_LEAF - MIN_LEAF;

pub fn count_utf16(text: &str) -> usize {
    text.encode_utf16().count()
}

pub fn split_for_insert(text: &str) -> usize {
    compute_split_point(text, MIN_LEAF)
}

pub fn split_for_merge(text: &str) -> usize {
    let preferred = text.len().saturating_sub(MAX_LEAF).max(MIN_LEAF);
    compute_split_point(text, preferred)
}

pub fn fits_within_capacity(len: usize) -> bool {
    len <= MAX_LEAF
}

fn compute_split_point(text: &str, preferred: usize) -> usize {
    if text.is_empty() {
        return 0;
    }
    if text.len() <= MAX_LEAF {
        return text.len();
    }

    let bounded_preferred = preferred.clamp(MIN_LEAF, MAX_LEAF);
    let newline_window_end = bounded_preferred
        .saturating_add(NEWLINE_WINDOW)
        .min(MAX_LEAF);
    let upper_bound = newline_window_end.min(text.len());
    let lower_bound = bounded_preferred.min(text.len().saturating_sub(MIN_LEAF));

    if let Some(split) = find_newline(text, lower_bound, upper_bound) {
        return split;
    }

    trim_to_char_boundary(text, upper_bound)
}

fn find_newline(text: &str, lower: usize, upper: usize) -> Option<usize> {
    if upper <= lower {
        return None;
    }
    let bytes = text.as_bytes();
    let mut idx = upper;
    while idx > lower {
        idx -= 1;
        if bytes[idx] == ASCII_NEWLINE {
            let candidate = idx + 1;
            if text.is_char_boundary(candidate) {
                return Some(candidate);
            }
        }
    }
    None
}

fn trim_to_char_boundary(text: &str, upper: usize) -> usize {
    if upper == 0 {
        return 0;
    }
    let mut split = upper.min(text.len()).min(MAX_LEAF);
    while split > 0 && !text.is_char_boundary(split) {
        split -= 1;
    }
    if split < MIN_LEAF {
        split = MIN_LEAF.min(text.len());
        while split < text.len() && !text.is_char_boundary(split) {
            split += 1;
        }
    }
    split.min(text.len())
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn split_prefers_newline_within_window() {
        let prefix = "a".repeat(MIN_LEAF + 4);
        let payload = format!("{}\n{}", prefix, "b".repeat(MAX_LEAF));
        let split = split_for_insert(&payload);
        assert!(split > MIN_LEAF);
        assert_eq!(payload.as_bytes()[split - 1], ASCII_NEWLINE);
        assert!(split <= MAX_LEAF);
    }

    #[test]
    fn split_trims_to_char_boundary() {
        let payload = format!("{}😀{}", "x".repeat(MAX_LEAF), "y".repeat(MIN_LEAF));
        let split = split_for_merge(&payload);
        assert!(payload.is_char_boundary(split));
    }

    #[test]
    fn utf16_counter_matches_std() {
        let sample = "Line🌟\nBorrowed😀Text";
        assert_eq!(count_utf16(sample), sample.encode_utf16().count());
    }
}
