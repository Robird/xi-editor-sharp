use blocking_model_core::{BlockingPointKind, BlockingPointRegistry};

#[test]
fn registry_should_cover_expected_points() {
    let all = BlockingPointRegistry::all();
    assert_eq!(6, all.len());
}

#[test]
fn registry_should_allow_lookup() {
    let spec = BlockingPointRegistry::find(BlockingPointKind::CursorLifecycle).unwrap();
    assert!(spec.objective.contains("cursor"));
}
