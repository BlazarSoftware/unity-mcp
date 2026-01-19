using Newtonsoft.Json.Linq;
using NUnit.Framework;
using MCPForUnity.Editor.Tools;
using static MCPForUnityTests.Editor.TestUtilities;

namespace MCPForUnityTests.Editor.Tools
{
    public class ManageTimelineTests
    {
        [Test]
        public void CheckTimelinePackage_ReturnsPackageStatus()
        {
            // Act
            var result = ToJObject(ManageTimeline.HandleCommand(new JObject
            {
                ["action"] = "check_package"
            }));

            // Assert
            Assert.IsTrue(result.Value<bool>("success"), result.ToString());
            var data = result["data"] as JObject;
            Assert.IsNotNull(data);
            Assert.IsNotNull(data["hasTimeline"]);
            Assert.IsNotNull(data["recommendation"]);
        }
    }
}
