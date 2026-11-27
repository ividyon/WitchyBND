using SoulsFormats;
using WitchyBND.Services;
using WitchyLib;
using WitchyTests.Services;

namespace WitchyTests;

public class GameServiceTests
{
    [Test]
    public void DeterminesParamGameFromProjectWhenBinderHasNoGameElement()
    {
        var root = Path.Combine(TestContext.CurrentContext.WorkDirectory, "Results", Guid.NewGuid().ToString("N"));
        var binder = Path.Combine(root, "gameparam-parambnd-dcx");
        Directory.CreateDirectory(binder);
        File.WriteAllText(Path.Combine(root, "project.json"), """
            {
              "GameType": 6,
              "GameRoot": "."
            }
            """);
        File.WriteAllText(Path.Combine(binder, "_witchy-bnd4.xml"), "<bnd4><version>07D7R6</version></bnd4>");

        var output = new TestOutputService();
        var gameService = new GameService(new ErrorService(output), output);
        // Use a populated cache so this test needs no Paramdex files.
        gameService.ParamdefStorage[WBUtil.GameType.BB]["Fixture"] = new PARAMDEF();
        var (game, _) = gameService.DetermineGameType(
            Path.Combine(binder, "Fixture.param"), IGameService.GameDeterminationType.PARAM, regVer: 109);
        Assert.That(game, Is.EqualTo(WBUtil.GameType.BB));
    }
}
