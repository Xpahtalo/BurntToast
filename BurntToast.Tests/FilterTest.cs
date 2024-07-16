using System.Collections.Generic;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using Xunit;

namespace BurntToast.Tests;

[TestSubject(typeof(Filter))]
public class FilterTest {
    private const string NoLongerSelling = "You are no longer selling";
    private const string NowSelling      = "You are now selling";
    private const string AssignRetainer  = "You assign your retainer";
    private const string NowComplete     = "is now complete";
    private const string GilEarned       = "Gil earned from market sales has been entrusted to your retainer";
    private const string Equipped        = "equipped";
    private const string Crop            = "This crop is doing well";
    private const string Appearance      = "takes on the appearance of";

    private const string EligibleForRewards =
        "party members are eligible for duty rewards. The number of coffers to appear will be ";
    private static List<Regex> MyPatterns => [
        new Regex(NoLongerSelling,    RegexOptions.Compiled), new Regex(NowSelling,  RegexOptions.Compiled),
        new Regex(AssignRetainer,     RegexOptions.Compiled), new Regex(NowComplete, RegexOptions.Compiled),
        new Regex(GilEarned,          RegexOptions.Compiled), new Regex(Equipped,    RegexOptions.Compiled),
        new Regex(Crop,               RegexOptions.Compiled), new Regex(Appearance,  RegexOptions.Compiled),
        new Regex(EligibleForRewards, RegexOptions.Compiled),
    ];

    private static List<Regex> ReportedProblemPatterns => [
        new Regex(string.Empty, RegexOptions.Compiled), new Regex("Invaid Target", RegexOptions.Compiled),
    ];


    private static List<BattleTalkPattern> ReportedTalkProblemPatterns => [
        new BattleTalkPattern(string.Empty, false), new BattleTalkPattern("Invaid Target", false),
    ];


    [Theory]
    [InlineData("You are no longer selling items in the Ishgard markets.",       NoLongerSelling, HandledType.Blocked)]
    [InlineData("You are no longer selling items in the Limsa Lominsa markets.", NoLongerSelling, HandledType.Blocked)]
    [InlineData("You assign your retainer “Quick Exploration.”",                 AssignRetainer,  HandledType.Blocked)]
    [InlineData("You are now selling items in the Ishgard markets.",             NowSelling,      HandledType.Blocked)]
    [InlineData("Whyamipayingforthis has reached maximum level.",                "",              HandledType.Passed)]
    public void RetainerToasts(string toast, string expectedString, HandledType expectedHandled) {
        Assert.Equal((expectedString, expectedHandled), Filter.FindPatternMatch(toast, MyPatterns));
    }

    [Theory]
    [InlineData("You are no longer selling items in the Ishgard markets.",       "", HandledType.Passed)]
    [InlineData("You are no longer selling items in the Limsa Lominsa markets.", "", HandledType.Passed)]
    [InlineData("You assign your retainer “Quick Exploration.”",                 "", HandledType.Passed)]
    [InlineData("You are now selling items in the Ishgard markets.",             "", HandledType.Passed)]
    [InlineData("Whyamipayingforthis has reached maximum level.",                "", HandledType.Passed)]
    public void ProblemPatterns(string toast, string expectedString, HandledType expectedHandled) {
        Assert.Equal((expectedString, expectedHandled), Filter.FindPatternMatch(toast, ReportedProblemPatterns));
    }

    [Theory]
    [InlineData("You are no longer selling items in the Ishgard markets.",       "", HandledType.Passed)]
    [InlineData("You are no longer selling items in the Limsa Lominsa markets.", "", HandledType.Passed)]
    [InlineData("You assign your retainer “Quick Exploration.”",                 "", HandledType.Passed)]
    [InlineData("You are now selling items in the Ishgard markets.",             "", HandledType.Passed)]
    [InlineData("Whyamipayingforthis has reached maximum level.",                "", HandledType.Passed)]
    public void TalkTest(string talk, string expectedString, HandledType expectedHandled) {
        Assert.Equal(
            (expectedString, expectedHandled, false), Filter.FindBattleTalkMatch(talk, ReportedTalkProblemPatterns));
    }
}