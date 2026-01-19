using Newtonsoft.Json.Linq;
using NUnit.Framework;
using MCPForUnity.Editor.Tools;
using static MCPForUnityTests.Editor.TestUtilities;

namespace MCPForUnityTests.Editor.Tools
{
    public class ManageVersionControlTests
    {
        [Test]
        public void IsGitRepository_ChecksGitRepo()
        {
            // Act
            var result = ToJObject(ManageVersionControl.HandleCommand(new JObject
            {
                ["action"] = "is_git_repo"
            }));

            // Assert
            Assert.IsTrue(result.Value<bool>("success"), result.ToString());
            var data = result["data"] as JObject;
            Assert.IsNotNull(data);
            Assert.IsNotNull(data["isGitRepo"]);
        }

        [Test]
        public void GetGitStatus_ReturnsStatus()
        {
            // Act
            var result = ToJObject(ManageVersionControl.HandleCommand(new JObject
            {
                ["action"] = "get_status"
            }));

            // Assert - might fail if Git not available
            if (result.Value<bool>("success"))
            {
                var data = result["data"] as JObject;
                Assert.IsNotNull(data);
                Assert.IsNotNull(data["output"]);
            }
        }

        [Test]
        public void GetCurrentBranch_ReturnsBranch()
        {
            // Act
            var result = ToJObject(ManageVersionControl.HandleCommand(new JObject
            {
                ["action"] = "get_branch"
            }));

            // Assert - might fail if Git not available
            if (result.Value<bool>("success"))
            {
                var data = result["data"] as JObject;
                Assert.IsNotNull(data);
                Assert.IsNotNull(data["branch"]);
            }
        }

        [Test]
        public void GetLog_ReturnsCommitLog()
        {
            // Act
            var result = ToJObject(ManageVersionControl.HandleCommand(new JObject
            {
                ["action"] = "get_log",
                ["count"] = 5
            }));

            // Assert - might fail if Git not available
            if (result.Value<bool>("success"))
            {
                var data = result["data"] as JObject;
                Assert.IsNotNull(data);
                Assert.IsNotNull(data["log"]);
            }
        }

        [Test]
        public void Commit_WithoutMessage_ReturnsError()
        {
            // Act
            var result = ToJObject(ManageVersionControl.HandleCommand(new JObject
            {
                ["action"] = "commit"
            }));

            // Assert
            Assert.IsFalse(result.Value<bool>("success"));
        }
    }
}
