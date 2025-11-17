use blocking_model_core::skeleton::prelude::*;

#[test]
fn cursor_descriptor_roundtrip() {
    let rope = sample_rope();
    let cursor = rope.cursor();
    let descriptor = cursor.to_descriptor();
    let restored = rope.apply_descriptor(descriptor.clone());
    assert_eq!(
        cursor.to_descriptor().summary(),
        restored.to_descriptor().summary()
    );
}

#[test]
fn shared_node_clone_preserves_info() {
    let node: Node<SampleNodeInfo, SampleLeaf> = Node::new(SampleNodeInfo::new(42));
    let shared = SharedNode::new(node);
    let cloned = shared.clone_handle();
    assert_eq!(
        shared.as_ref().info().describe_cursor(),
        cloned.as_ref().info().describe_cursor()
    );
}
