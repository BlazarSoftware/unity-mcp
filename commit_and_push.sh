#!/bin/bash

# Navigate to repository root
cd "$(dirname "$0")"

echo "=== Git Status ==="
git status --short

echo ""
echo "=== Staging all new and modified files ==="
git add MCPForUnity/Editor/Tools/ManagePlayMode.cs
git add MCPForUnity/Editor/Tools/ManagePlayMode.cs.meta
git add MCPForUnity/Editor/Tools/ManageProfiler.cs
git add MCPForUnity/Editor/Tools/ManageCompilation.cs
git add MCPForUnity/Editor/Tools/ManageAssetImport.cs
git add MCPForUnity/Editor/Tools/ManageHistory.cs
git add MCPForUnity/Editor/Tools/ManageQualitySettings.cs
git add MCPForUnity/Editor/Tools/ManageSearch.cs
git add MCPForUnity/Editor/Tools/ManageSceneView.cs
git add MCPForUnity/Editor/Tools/ManageAnimation.cs
git add MCPForUnity/Editor/Tools/ManageVersionControl.cs
git add MCPForUnity/Editor/Tools/ManageInput.cs
git add MCPForUnity/Editor/Tools/ManageTimeline.cs
git add MCPForUnity/Editor/Tools/ManageAddressables.cs
git add MCPForUnity/Editor/Tools/ManageVisualScripting.cs

git add MCPForUnity/Editor/Tests/Tools/ManagePlayModeTests.cs
git add MCPForUnity/Editor/Tests/Tools/ManagePlayModeTests.cs.meta
git add MCPForUnity/Editor/Tests/Tools/ManageProfilerTests.cs
git add MCPForUnity/Editor/Tests/Tools/ManageCompilationTests.cs
git add MCPForUnity/Editor/Tests/Tools/ManageAssetImportTests.cs
git add MCPForUnity/Editor/Tests/Tools/ManageHistoryTests.cs
git add MCPForUnity/Editor/Tests/Tools/ManageQualitySettingsTests.cs
git add MCPForUnity/Editor/Tests/Tools/ManageSearchTests.cs
git add MCPForUnity/Editor/Tests/Tools/ManageSceneViewTests.cs
git add MCPForUnity/Editor/Tests/Tools/ManageAnimationTests.cs
git add MCPForUnity/Editor/Tests/Tools/ManageVersionControlTests.cs
git add MCPForUnity/Editor/Tests/Tools/ManageInputTests.cs
git add MCPForUnity/Editor/Tests/Tools/ManageTimelineTests.cs
git add MCPForUnity/Editor/Tests/Tools/ManageAddressablesTests.cs
git add MCPForUnity/Editor/Tests/Tools/ManageVisualScriptingTests.cs

git add MCPForUnity/Editor/Resources/Packages/PackageEvents.cs
git add MCPForUnity/Editor/Tests/Resources/PackageEventsTests.cs

git add NEW_FEATURES_SUMMARY.md

echo ""
echo "=== Files staged ==="
git status --short

echo ""
echo "=== Committing with detailed message ==="
git commit -F COMMIT_MESSAGE.txt

echo ""
echo "=== Commit complete ==="
git log -1 --stat

echo ""
echo "=== Pushing to remote ==="
git push origin blazar-main

echo ""
echo "=== Push complete! ==="
echo "All 15 new features have been committed and pushed to the server."
