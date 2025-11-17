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
