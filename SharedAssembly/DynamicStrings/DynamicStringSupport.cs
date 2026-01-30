using System.Text.RegularExpressions;
using System.Text;
using System.Linq;

namespace SharedAssembly.DynamicStrings;

public class DynamicStringSupport
{

    public static bool IsSafeContract(DynamicStringContract contract, bool skipCombos = false)
    {
        //return contract.Method == "OnButtonClick";

        string[] skipTypes = [
            // Game Components
            "Automat",
            "CustomDataInfo",
            "SpawnPointPrototype",
            "SystemIntroduce",
            //"DownloadView",
            //"PlayerStateComponent",
            //"DataManager",
            //"FashionComponent",
            //"BuildEntity",
            "GmManager",
            //"AIDialogView",
            //"EndingView",
            "UploadFile",
            "DataMgr",
            //"EditWayPointData",
            //"AppGame",
            //"Demolish",
            //"Excalibur",
            "JHPerfSight",
            //"MapAreaEvent",
            "MouseSimulator",
            "NpcChatView",
            //"NpcRoleNodeNew",
            "OpenFileName",
            "PatchManager",
            "ScreenShotFaceParam",
            "StuntManager", //Causes issues - was CustomData
            "TestManagedHeap",
            "VirtualEvent",
            "VirtualGrid",
            "WaveController",
            "SweetPotato.WordEntryLooter",
            //"SweetPotato.WorldManager",
            "SweetPotato.WordEntryGlobalManager",
            "SweetPotato.NpcPrototypeBaseRandom",
            "FloydManager",
            "UnityHelper", //Manually implemented
            "PolygonDrawer",
            //"EditEntrustEventPointData",
            //"EditWayPointData",
            "RandomNpcManager",
            "MyTimeline",
            //"DLCView",
            //"DLCShowViewItemHolder",
            //"DLCShowView",

            // Looks like modding ui
            //"EditNpcData",
            "EditorDataPanel",
            //"EditSpwData",
            //"EditTeleData",
            "MapAreaEditor",
            //"ModSpace",
            //"SystemIntroduceView",
            //"QuestPrototypeLineItemSlot2",
            //"EditBornData",
            //"EditTriggerData",           

            //Might be corrupting quests
            //"ItemPrototype",
            
            //Valid but needs to be debugged:
            //"RelationShipManager",
        ];

        string[] skipMethods = [
            "CheckTapAnti",
            "CheckDBVersionAndDownloadFromServer",
            "IsSaveableNow",
            "LoadCSV",
            "LoadDB",
            "CreateFromCsvRow",
            "OnButtonClick", //Bad stuff happens here
            "EnterNewHour", //Needs to check out
         ];

        // TODO: Figure out why these dont patch
        string[] skipCombinations = [

            "SweetPotato.MiniGames.GeZi1.GeZiGame.InitGame", //Can't find
            "InteractSlot.Handle_IT_SCRIPT", //Debug
            "SweetPotato.Tools.GetIconSprite", //Named sprites
            "SweetPotato.Tools.GetShopItemIconByQuality", //Named sprites
            "RoleBagItemListHolder.GetIconByQua", //Named sprites
            "SweetPotato.SpellEditView.OnEnable", //Debug
            "SweetPotato.ConditionPrototype.UpdateAchievedState", //Causes Quest Bug 确定 and 取消 being defaulted on function call
            "SweetPotato.PlayerQuest.GetFinishConditonTarget", //Causes Quest Bug 确定 and 取消 being defaulted on function call
            "ItemPrototype.InitRandomItems", //debug
            "LootItem.Init", //debug
            "SweetPotato.NpcEntity.Init",  //debug
            "SweetPotato.NpcEntity.InitNpcAtt",  //debug
            "SweetPotato.NpcEntity.InitNpcSpell",  //debug
            "ShopStoreSystem.OnEnterShopMode", //Debug

            //Thing needing reimplementation
            "UnityHelper.GetNunWordFromNum",  //Done
            "SweetPotato.Tools.NumberToChinese", //Done
            //"SweetPotato.Tools.GetGameTimeDate", //Done
            //"SweetPotato.Tools.GetRealTimeDate2", //Done
            //"SweetPotato.Tools.GetRemainTimeDate", //Done
            //"SweetPotato.Tools.GetTaskGameTimeDate", //Done
            //"SweetPotato.Tools.GetXiaoxiGameTimeDate" //Done

            //Dicey
            //SweetPotato.Spell.do_effect <-- it adds effect param but it could be going to a sprite thats not showing - need to find out

            //Unknown
            "SweetPotato.YunBiao.BiaoBasicInfo.GetInfo",

            //Unknown
            "MenPai.GetInfoById",
            "MiJiMgr.Add",
            "NewChuanWenView/<OnShowChuanWen>d__18.MoveNext",
            "SweetPotato.AutomatScriptManager.PrintExeFuctionScript",
            "SweetPotato.ConditionPrototype.ParseCondition",            
            "SweetPotato.InstantiateViewNewNew_mobile/<EnterCharacterLine>d__111.MoveNext",
            "SweetPotato.InstantiateViewNewNew_mobile/<ShowLoginMoveTMLNEW_NEW>d__112.MoveNext",
            "SweetPotato.JingMai_ZhouTianEffect.GetEffectIdList",
            "SweetPotato.JingMai_ZhouTianEffect.GetEffectStrByType",
            "SweetPotato.LoginViewNew/<AsyncSetLoadVirtualCamera>d__31.MoveNext",
            "SweetPotato.MenPaiEntity.NpcGetCurMonthGongZi",
            "SweetPotato.MiJiPage.Load",
            "SweetPotato.NpcAttriDynamicExtralTemplete.GetInfo",           
            "SweetPotato.NpcSpellDynamic.GetInfo",
            "SweetPotato.NpcSpellDynamic.Init",
            "SweetPotato.NpcSpellDynamic.InitNpcSpell",
            "SweetPotato.RandomEventsStorage/CompleteState.ShowResult",
            "SweetPotato.RandomEventsStorage/WaitCompleteState.OnCheckRandomEventState",
            "SweetPotato.RandomEventsStorage/WaitCompleteState.OnEnter",
            "SweetPotato.SpawnPointChild.CreateUnitEntity",
            "SweetPotato.Spell.CheckEffectCond",
            "SweetPotato.Spell.PlayCastAnim",           
        ];

        var combo = $"{contract.Type}.{contract.Method}";

        if (skipTypes.Contains(contract.Type))
            return false;

        foreach (var type in skipTypes)
            if (contract.Type.Contains(type))
                return false;

        if (skipMethods.Contains(contract.Method))
            return false;

        // These are used purely for hiding stuff we dont know what it does yet
        if (!skipCombos && skipCombinations.Contains(combo))
            return false;

        return true;
    }

    public static string[] PrepareMethodParameters(string split)
    {
        var rawParameters = split[1..^1] //Remove square brackets
            .Replace("，", ","); // Replace Wide quotes back

        string splitPattern = @",(?![^\[\]{}<>]*[\]\}>])";
        string replacement = "，";  // Change to whatever you want

        // Replace for generics because split regex is flakey
        rawParameters = ReplaceCommasInBrackets(rawParameters, replacement);

        var parameters = Regex.Split(rawParameters, splitPattern) ?? [];

        // Replace back for generics
        for (int i = 0; i < parameters.Length; i++)
            parameters[i] = parameters[i].Replace(replacement, ",");

        //parameters = parameters.Where(p => !string.IsNullOrEmpty(p)).ToArray(); -- Serialises wierd
        return parameters;
    }

    static string ReplaceCommasInBrackets(string input, string replacement)
    {
        var output = new StringBuilder();
        int depth = 0;

        foreach (char c in input)
        {
            if (c == '<' || c == '[' || c == '{')
                depth++;  // Entering a bracketed section

            if (c == '>' || c == ']' || c == '}')
                depth--;  // Exiting a bracketed section

            if (c == ',' && depth > 0)
                output.Append(replacement);  // Replace ',' only inside brackets
            else
                output.Append(c);
        }

        return output.ToString();
    }
}
