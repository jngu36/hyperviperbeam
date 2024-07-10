/*using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SamplePlugin.Windows
{
    internal class ViperBar
    {
    }
}
*/

using System;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Transactions;
using Dalamud.Game.ClientState.JobGauge.Types;
using Dalamud.Game.ClientState.Structs;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Gauge;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using static FFXIVClientStructs.FFXIV.Client.UI.Misc.GroupPoseModule;

namespace ViperBar.Windows;


public class ViperConfig : Window, IDisposable
{

    /*
    Self notes:
    action id

	34606 - Steel Fangs - 1
	34607 - Dread Fangs - 2

	34608 - Hunter sting - 1
	34609 - Swiftskin's Sting - 2

	34610 - Flanksting Strike - 11
	34611 - Flanksbane Fang - 12

	34612 - Hindsting Strike - 21
	34613 - Hindsbane Fang - 22

	buff id

	3647 - hindstung Venom
	3646 - flanksbane venom
	3648 - hindsbane venom
	3645 - Flankstung Venom

	3655 - Death Rattle Ready!
	
	3670 - Awakening buff
	3662 - Rattle awake?

 */

    private Plugin plugin;
    private IClientState clientState;
    private IPluginLog pluginLog;
    private readonly uint[] viperbuffs = [3647, 3646, 3648, 3645];

    private uint lastActionId;
    private float lastActionRecast;

    private string left = "";
    private string right = "";

    // We give this window a hidden ID using ##
    // So that the user will see "My Amazing Window" as window title,
    // but for ImGui the ID is "My Amazing Window##With a hidden ID"
    public ViperConfig(Plugin plug, IClientState cs, IPluginLog pl)
        : base("Viper Debug Window##With a hidden ID",
            ImGuiWindowFlags.NoScrollbar |
            ImGuiWindowFlags.NoScrollWithMouse)
            //ImGuiWindowFlags.NoBackground)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(350, 200),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        plugin = plug;
        clientState = cs;
        pluginLog = pl;

    }

    public void Dispose() { }

    public override void Draw()
    {
        if (isViper())
        {
            ImGui.Text($"Hello");
            ImGui.Text($"Last Action: {lastActionId}");
            ImGui.Text($"Recast Time: {lastComboActionRecast()}");
            ImGui.Spacing();
            ImGui.Text($"Rattling Coil: {GetRattleStacks()}");
            //ImGui.Text($"Serpent Offering: {GetSerpentStacks()}");

            //ImGui.Text($"{getBuffName(getBuff())}");
            //ImGui.Text($"{direction(getBuff())}");
            ImGui.Spacing();
            if (animeStacks() > 0)
            {
                ImGui.Text($"ANIME POWER UP TIME: {animeStacks()}");
            }
            else
            {
                //testing

                ImGui.Text($"{left} {GetSerpentStacks()} {right}");
            }
        }
        else
        {
            ImGui.Text($"hiss >:C");
        }

    }

    //check if class is viper
    public unsafe bool isViper()
    {
        return PlayerState.Instance()->CurrentClassJobId == 41;
    }

    private unsafe float lastComboActionRecast()
    {
        var instance = ActionManager.Instance();
        lastActionId = instance->Combo.Action;
        lastActionRecast = instance->GetRecastTime(ActionType.Action, lastActionId) -
            instance->GetRecastTimeElapsed(ActionType.Action, lastActionId);
        LeftRightCheck();

        return lastActionRecast;
    }


    //left right indicators

    private unsafe void LeftRightCheck()
    {
        if (lastActionId == 34610 | //Flanksting Strike
                lastActionId == 34611 | //Flanksbane Fang
                lastActionId == 34612 | //Hindsting Strike
                lastActionId == 34613) //Hindsbane Fang 
        {
            left = "    ";
            right = "";
        }
        else if (lastActionId == 34606 | lastActionId == 34607) // Steel OR Dread fangs
        {
            left = IsHunterStingHighlighted() ? "left" : "    ";
            right = IsSwiftSkingHighlighted() ? "right" : "";
        }
        if (lastActionId == 34608) // Hunter sting
        {
            left = IsFlankstingHighlighted() ? "left" : "    ";
            right = IsFlankbaneHighlighted() ? "right" : "";

        }
        if (lastActionId == 34609) //Swiftskin's Sting
        {
            left = IsHindstingHighlighted() ? "left" : "    ";
            right = IsHindsbaneHighlighted() ? "right" : "";
        }
    }


    //left side check

    //2nd
    private unsafe bool IsHunterStingHighlighted()
    {
        var instance = ActionManager.Instance();
        return instance->IsActionHighlighted(ActionType.Action, 34608);
    }

    //flank
    private unsafe bool IsFlankstingHighlighted()
    {
        var instance = ActionManager.Instance();
        return instance->IsActionHighlighted(ActionType.Action, 34610);
    }

    //rear 
    private unsafe bool IsHindstingHighlighted()
    {
        var instance = ActionManager.Instance();
        return instance->IsActionHighlighted(ActionType.Action, 34612);
    }


    //right side check

    //2nd
    private unsafe bool IsSwiftSkingHighlighted()
    {
        var instance = ActionManager.Instance();
        return instance->IsActionHighlighted(ActionType.Action, 34609);
    }

    //flank
    private unsafe bool IsFlankbaneHighlighted()
    {
        var instance = ActionManager.Instance();
        return instance->IsActionHighlighted(ActionType.Action, 34611);
    }

    //rear
    private unsafe bool IsHindsbaneHighlighted()
    {
        var instance = ActionManager.Instance();
        return instance->IsActionHighlighted(ActionType.Action, 34613);
    }


    //Resource gets

    public unsafe int GetRattleStacks()
    {
        var instance = JobGaugeManager.Instance();

        return isViper() ?
            instance->Viper.RattlingCoilStacks
            : -1;

    }

    public unsafe int GetSerpentStacks()
    {
        var instance = JobGaugeManager.Instance();

        return isViper() ?
            instance->Viper.SerpentOffering
            : -1;
    }

    public unsafe int animeStacks()
    {
        var instance = JobGaugeManager.Instance();

        return isViper() ?
            instance->Viper.AnguineTribute
            : -1;
    }


    public unsafe uint getBuff()
    {
        uint buff = 0;

        var lp = clientState.LocalPlayer;
        if (lp != null)
            foreach (var s in lp.StatusList)
            {
                buff = Array.Find(viperbuffs, e => e == s.StatusId);
                if (buff != 0)
                    break;
            }
        return buff;
    }

    private unsafe string getBuffName(uint buffId)
    {
        return buffId switch
        {
            3645 => "Flankstung Venom(Left)",
            3646 => "Flanksbane Venom(Right)",
            3647 => "Hindstung Venom(Left)",
            3648 => "Hindsbane Venom(Right)",
            _ => ""
        };
    }

    private unsafe string direction(uint buffId)
    {
        return buffId switch
        {
            3645 => " • Left, Left (Flank)",
            3646 => " • Left, Right (Flank)",
            3647 => " • Right, Left (Rear)",
            3648 => " • Right, Right (Rear)",
            _ => ""
        };
    }

    public unsafe string testFunc()
    {

        uint buff = 0;
        var found = "";

        var instance = ActionManager.Instance();
        var c = instance->Combo.Action.ToString();

        switch (buff)
        {
            case 3645:
                found = "Flankstung Venom (Left)";
                break;
            case 3646:
                found = "Flanksbane Venom (Right)";
                break;
            case 3647:
                found = "Hindstung Venom (Left)";
                break;
            case 3648:
                found = "Hindsbane Venom (Right)";
                break;
            default:
                found = "";
                break;
        }


        Log(found);
        return found;
    }

    public void Log(string msg) => pluginLog.Debug(msg);
}

