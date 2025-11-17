use blocking_model_core::skeleton::prelude::*;

#[test]
fn cursor_descriptor_roundtrip() {
    let rope = sample_rope_via_builder();
    let cursor = rope.cursor();
    let descriptor = cursor.to_descriptor();
    let restored = rope.apply_descriptor(descriptor.clone());
    assert_eq!(descriptor, restored.to_descriptor());
}

#[test]
fn shared_node_clone_preserves_info() {
    let node: Node<SampleNodeInfo, SampleLeaf> = Node::from_leaf(SampleLeaf::from_str("len"));
    let shared = node.clone().into_shared();
    let cloned = shared.clone_handle();
    assert_eq!(shared.len(), cloned.len());
}

#[test]
fn tree_builder_trace_export_respects_feature_gate() {
    let mut builder: TreeBuilder<SampleNodeInfo, SampleLeaf> =
        TreeBuilder::with_tracer(TreeBuilderTracer::new());
    builder.set_tracer_enabled(true);
    builder.push_leaf(SampleLeaf::from_str("alpha"));
    builder.push_leaf(SampleLeaf::from_str("beta"));
    let snapshot = builder.export_trace().expect("tracer must exist");
    if cfg!(feature = "tree_builder_slice_trace") {
        assert!(snapshot
            .events()
            .iter()
            .any(|event| matches!(event, TreeBuilderEvent::PushLeaf { .. })));
    } else {
        assert!(snapshot.events().is_empty());
    }
    let (_root, tracer) = builder.build_with_tracer();
    let trace = tracer.expect("missing tracer").export();
    if cfg!(feature = "tree_builder_slice_trace") {
        assert!(trace
            .events()
            .iter()
            .any(|event| matches!(event, TreeBuilderEvent::BuildComplete { .. })));
    } else {
        assert!(trace.events().is_empty());
    }
}

#[test]
fn default_metric_provider_roundtrips_utf16_units() {
    let node: Node<SampleNodeInfo, SampleLeaf> =
        Node::from_leaf(SampleLeaf::from_str("\u{1F30D} sample"));
    let utf16_units = SampleNodeInfo::convert_from_default::<Utf16Metric>(&node, node.len());
    let base_units = SampleNodeInfo::convert_to_default::<Utf16Metric>(&node, utf16_units);
    assert_eq!(base_units, node.len());
}

#[test]
fn rope_edit_rebuilds_text_with_helpers() {
    let mut rope = sample_rope_via_builder();
    let interval = Interval::new(4, 10);
    rope.edit(interval, "++");
    assert_eq!(rope_text(&rope), "left++right");
}

#[test]
fn rope_slice_returns_interval_contents() {
    let rope = sample_rope_via_builder();
    let sliced = rope.slice(Interval::new(4, 10));
    assert_eq!(rope_text(&sliced), "middle");
}

#[test]
fn deep_tree_sample_supports_cursor_roundtrip() {
    let (rope, trace) = sample_deep_tree_rope(3);
    let cursor = rope.cursor();
    let descriptor = cursor.to_descriptor();
    let restored = rope.apply_descriptor(descriptor.clone());
    assert_eq!(descriptor, restored.to_descriptor());
    let trace = trace.expect("deep tree sample must return trace");
    if cfg!(feature = "tree_builder_slice_trace") {
        assert!(!trace.events().is_empty());
    } else {
        assert!(trace.events().is_empty());
    }
}

#[cfg(feature = "tree_builder_slice_trace")]
#[test]
fn tree_builder_trace_serializes_to_json_payload() {
    if !cfg!(feature = "serde_json") {
        eprintln!("serde_json feature disabled; skipping serialization test");
        return;
    }

    #[cfg(feature = "serde_json")]
    {
        let (_rope, trace) = sample_deep_tree_rope(2);
        let trace = trace.expect("trace missing under trace feature");
        let json = trace
            .to_json_string()
            .expect("failed to serialize tree builder trace");
        assert!(json.contains("PushLeaf"));
        assert!(json.contains("BuildComplete"));
    }
}

#[cfg(feature = "cursor_state")]
#[test]
fn cursor_state_roundtrip() {
    let rope = sample_rope_via_builder();
    let cursor = rope.cursor();
    let state = cursor.state();
    let restored = rope.apply_descriptor(state.to_descriptor());
    assert_eq!(state.descriptor(), restored.state().descriptor());
}

fn rope_text(rope: &Rope<SampleNodeInfo, SampleLeaf>) -> String {
    fn collect(node: &SharedNode<SampleNodeInfo, SampleLeaf>, buffer: &mut String) {
        match &node.as_ref().val {
            NodeVal::Leaf(leaf) => buffer.push_str(leaf.as_str()),
            NodeVal::Internal(children) => {
                for child in children {
                    collect(child.shared(), buffer);
                }
            }
        }
    }

    let mut buffer = String::new();
    collect(rope.root(), &mut buffer);
    buffer
}
