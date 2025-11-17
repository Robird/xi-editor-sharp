#![cfg(feature = "trace_cli")]

use assert_cmd::cargo::cargo_bin_cmd;

#[test]
fn cli_outputs_json_brackets_for_text_input() {
    let mut cmd = cargo_bin_cmd!("export-tree-builder-trace");
    let output = cmd
        .args(["--text", "abc"])
        .output()
        .expect("failed to run export-tree-builder-trace");

    assert!(
        output.status.success(),
        "CLI exited with {:?}. stderr: {}",
        output.status.code(),
        String::from_utf8_lossy(&output.stderr)
    );

    let stdout = String::from_utf8_lossy(&output.stdout);
    assert!(stdout.contains('[') && stdout.contains(']'), "stdout: {stdout}");
}
