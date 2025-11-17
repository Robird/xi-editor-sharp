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
fn tree_builder_tracer_records_events() {
    let builder_tracer = TreeBuilderTracer::new();
    let mut builder: TreeBuilder<SampleNodeInfo, SampleLeaf> =
        TreeBuilder::with_tracer(builder_tracer);
    builder.push_leaf(SampleLeaf::from_str("alpha"));
    builder.push_leaf(SampleLeaf::from_str("beta"));
    let (_root, tracer) = builder.build_with_tracer();
    let events = tracer.expect("tracer should be returned").into_events();
    assert!(matches!(
        events.first(),
        Some(TreeBuilderEvent::PushLeaf { .. })
    ));
    assert!(
        events
            .iter()
            .any(|event| matches!(event, TreeBuilderEvent::EnterChild { .. }))
    );
    assert!(
        events
            .iter()
            .any(|event| matches!(event, TreeBuilderEvent::BuildComplete { .. }))
    );
}

#[test]
fn default_metric_provider_roundtrips_utf16_units() {
    let node: Node<SampleNodeInfo, SampleLeaf> =
        Node::from_leaf(SampleLeaf::from_str("\u{1F30D} sample"));
    let utf16_units = SampleNodeInfo::convert_from_default::<Utf16Metric>(&node, node.len());
    let base_units = SampleNodeInfo::convert_to_default::<Utf16Metric>(&node, utf16_units);
    assert_eq!(base_units, node.len());
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
