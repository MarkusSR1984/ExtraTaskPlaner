/* ExtraTaskPlaner.cs

by schwitz@sossau.com

Free to use as is in any way you want with no warranty.

*/

using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Collections;
using System.Net;
using System.Web;
using System.Data;
using System.Threading;
using System.Timers;
using System.Diagnostics;
using System.Reflection;

using PRoCon.Core;
using PRoCon.Core.Plugin;
using PRoCon.Core.Plugin.Commands;
using PRoCon.Core.Players;
using PRoCon.Core.Players.Items;
using PRoCon.Core.Battlemap;
using PRoCon.Core.Maps;


namespace PRoConEvents
{

//Aliases
using EventType = PRoCon.Core.Events.EventType;
using CapturableEvent = PRoCon.Core.Events.CapturableEvents;

public class ExtraTaskPlaner : PRoConPluginAPI, IPRoConPluginInterface
{

/* Inherited:
    this.PunkbusterPlayerInfoList = new Dictionary<string, CPunkbusterInfo>();
    this.FrostbitePlayerInfoList = new Dictionary<string, CPlayerInfo>();
*/
private enumBoolYesNo PluginDeveloper = enumBoolYesNo.No;
private enumBoolOnOff AdvancedMode = enumBoolOnOff.Off;

private bool IsEnabled;
private bool startupLock = false;
private int DebugLevel;
private int playerCount;
private int maxPlayerCount;
private int ServerUptime;
private Dictionary<string, PluginInfo> RegisteredPlugins = new Dictionary<string,PluginInfo>();
private Dictionary<int,Task> Taskmanager = new Dictionary<int,Task>();
private Dictionary<string, string> enumPluginCommands = new Dictionary<string, string>();
private Dictionary<string, string> enumPluginVariables = new Dictionary<string, string>();
private Dictionary<string, string> SavePluginVariables = new Dictionary<string, string>();
private string enumRemoveTaskList;
private string enumPluginAdd;
private string currentState;
private Task currentTask = new Task();


private MatchCommand matchCommandLookupRequest = new MatchCommand("ExtraTaskPlaner", "PluginInterface", new List<string>(), "ExtraTaskPlaner_PluginInterface", new List<MatchArgumentFormat>(), new ExecutionRequirements(ExecutionScope.None), "PluginInfo method for Task Manager. Cannot be called ingame");



public ExtraTaskPlaner() {
	IsEnabled = false;
	DebugLevel = 2;

    CreateTask("DEFAULT");
}

public void WritePluginConsole(string message, string tag, int level)
{
    try
    {

        if (tag == "ERROR")
        {
            tag = "^1" + tag;   // RED
        }
        else if (tag == "DEBUG")
        {
            tag = "^3" + tag;   // ORAGNE
        }
        else if (tag == "INFO")
        {
            tag = "^2" + tag;   // GREEN
        }
        else if (tag == "VARIABLE")
        {
            tag = "^6" + tag;   // GREEN
        }
        else if (tag == "WARN")
        {
            tag = "^7" + tag;   // PINK
        }


        else
        {
            tag = "^5" + tag;   // BLUE
        }

        string line = "^b[" + this.GetPluginName() + "] " + tag + ": ^0^n" + message;


        if (tag == "ENABLED") line = "^b^2" + line;
        if (tag == "DISABLED") line = "^b^3" + line;


        if (this.DebugLevel >= level)
        {
            this.ExecuteCommand("procon.protected.pluginconsole.write", line);
        }

    }
    catch (Exception e)
    {
        this.ExecuteCommand("procon.protected.pluginconsole.write", "^1^b[" + GetPluginName() + "][ERROR]^n WritePluginConsole: ^0" + e);
    }

}


public void ServerCommand(params String[] args)
{
	List<string> list = new List<string>();
	list.Add("procon.protected.send");
	list.AddRange(args);
	this.ExecuteCommand(list.ToArray());
}


public string GetPluginName() {
	return "Extra Task Manager";
}

public string GetPluginVersion() {
	return "0.0.0.1";
}

public string GetPluginAuthor() {
	return "MarkusSR1984";
}

public string GetPluginWebsite() {
	return "TBD";
}

public string GetPluginDescription() {
string PluginDescription = @"

If you find this plugin useful, please consider supporting me. Donations help support the servers used for development and provide incentive for additional features and new plugins! Any amount would be appreciated!</p>

<center>
<form action=""https://www.paypal.com/cgi-bin/webscr"" method=""post"" target=""_blank"">
<input type=""hidden"" name=""cmd"" value=""_s-xclick"">
<input type=""hidden"" name=""hosted_button_id"" value=""4VYFL94U9ME8L"">
<input type=""image"" src=""https://www.paypalobjects.com/en_US/i/btn/btn_donate_LG.gif"" border=""0"" name=""submit"" alt=""PayPal - The safer, easier way to pay online!"">
<img alt="""" border=""0"" src=""https://www.paypalobjects.com/de_DE/i/scr/pixel.gif"" width=""1"" height=""1"">
</form>
</center>

<h2>Description</h2>
This Plugin Controls other Plugins with Tasks. You can create some Tasks with various Types. But it does not support a controll of unkompatible Plugins! Plugin Developers enable the Plugin Developer Variable to get more informatin about you can make a supportet plugin here.

<h2>Commands</h2>
<p>This Plugin has no ingame Commands at the Moment</p>

<h2>Settings</h2>

<blockquote><h4>1. Taskmanager</h4>
	Add and Remove Tasks<br/>
</blockquote>

<blockquote><h4>Task 0 Default Configuration</h4>
	This Config is used when no other Task is active<br/>
    Some Variables are locked to guard them for editing<br/>
</blockquote>

<blockquote><h4>Task x Taskname</h4>
<pre>
Task_x_Enable           Enable or Disable this Task
Task_x_Name             Give this Task a Name
Task_x_Priority         Prioritys from 0 (lowest) to 100 (higest)
</pre>
With the Priority of a task can you controll with task is choosen if 2 tasks match at the same time
</blockquote>



<h3>Changelog</h3>
<blockquote><h4>0.0.0.1 (08.04.2014)</h4>
	- initial version<br/>
</blockquote>
";

if (PluginDeveloper == enumBoolYesNo.Yes)
{
    PluginDescription = PluginDescription + @"
    <h1>PLUGIN DEVELOPER INFOS</h1>
    <br/>
    <h2>How to use Extra Task Manager with your Plugin</h2>
    <br/>
    Its realy easy to use Extra Task Manager with your Plugin. You will find sample Code below.<br/>
    If you want to start a new Plugin you can use my Template. In this are all needed Methods build in.<br/>
    If you think you dont have any method for a Task Manager, Please Register your Plugin without any command or variable and give user the ability to Enable and Disable your Plugin by a Task<br/>
    And now, follow the instructions with sample code, the most is only copy and paste, and see your Plugin in Registered Plugin Info on the bottom of this Site<br/>


    <br/>
    <h2>Registered Plugin Infos</h2>
    ";
    if (RegisteredPlugins.Count > 0)
    {
        foreach (KeyValuePair<string, PluginInfo> Plugin in RegisteredPlugins)
        {
            PluginDescription += @"<blockquote><h4>" + Plugin.Key + @"</h4>";
            PluginDescription += @"<pre>";
            PluginDescription += @"<b>Plugin Name:</b>          " + Plugin.Value.PluginName + @"<br/>";
            PluginDescription += @"<b>Plugin Version:</b>       " + Plugin.Value.PluginVersion + @"<br/>";
            PluginDescription += @"<b>Plugin Class Name:</b>    " + Plugin.Value.PluginClassName + @"<br/>";
            PluginDescription += @"<b>Registered Commands:</b>  " + (Plugin.Value.Commands.Count).ToString() + @"<br/>";
            if (Plugin.Value.Commands.Count > 0)
            {
                foreach (string command in Plugin.Value.Commands)
                {
                    PluginDescription += @"<b>Command:</b> " + command + @"<br/>";
                }
            }

            PluginDescription += @"<b>Registered Variables:</b> " + (Plugin.Value.Variables.Count).ToString() + @"<br/>";
            if (Plugin.Value.Variables.Count > 0)
            {
                foreach (string variable in Plugin.Value.Variables)
                {
                    PluginDescription += @"<b>Variable:</b> " + variable + @"<br/>";
                }
            }
            PluginDescription += @"</pre>";
            PluginDescription += @"</blockquote>";
            
        }
    }

    PluginDescription += @"
    <h2>Sample Code</h2>
    Add all this Sample Code into your Plugin Class.<br/>
    I have splitted it in varios blocks to make it easy for you to find the right place<br/>
        
    <blockquote>
    <h4>Needed Variables</h4>
    
    private Hashtable PluginInfo = new Hashtable();<br/>
    private bool isRegisteredInTaskPlaner = false;<br/>
    private List&lt;string&gt; Commands = new List&lt;string&gt;();<br/>
    private List&lt;string&gt; Variables = new List&lt;string&gt;();<br/>    
    
    </blockquote>

    <blockquote>
    <h4>Needed Methods</h4>
    <pre>
    
public bool IsExtraTaskPlanerInstalled()
{
    List&lt;MatchCommand&gt; registered = this.GetRegisteredCommands();
    foreach (MatchCommand command in registered)
    {
        if (command.RegisteredClassname.CompareTo(""ExtraTaskPlaner"") == 0 && command.RegisteredMethodName.CompareTo(""PluginInterface"") == 0)
        {
            WritePluginConsole(""^bExtra Task Planer^n detected"", ""INFO"", 3);
            return true;
        }
        
    }

    return false;
}

public void ExtraTaskPlaner_Callback(string command)
{

    if (command == ""success"")
    {
        isRegisteredInTaskPlaner = true;
    }

    if (command == ""Sample Command"") doSomathing(); // Catched Command
    

}


private string GetCurrentClassName()
{
    string tmpClassName;
    
    tmpClassName =  this.GetType().ToString(); // Get Current Classname String
    tmpClassName = tmpClassName.Replace(""PRoConEvents."", """");
    
    
    return tmpClassName;

}
    
    
private void SendTaskPlanerInfo()
{

    Commands.Add(""Sample Command"");           // You have to catch this Commands in Method ExtraTaskPlaner_Callback()
    Commands.Add(""another Sample Command"");
        
    Variables.Add(""Sample Variable"");         // You have to catch this Variable in Method SetPluginVariable()
    Variables.Add(""Variable2"");





    PluginInfo[""PluginName""] = GetPluginName();
    PluginInfo[""PluginVersion""] = GetPluginVersion();
    PluginInfo[""PluginClassname""] = GetCurrentClassName();

    PluginInfo[""PluginCommands""] = CPluginVariable.EncodeStringArray(Commands.ToArray());
    PluginInfo[""PluginVariables""] = CPluginVariable.EncodeStringArray(Variables.ToArray());
          
    this.ExecuteCommand(""procon.protected.plugins.setVariable"", ""ExtraTaskPlaner"", ""RegisterPlugin"", JSON.JsonEncode(PluginInfo)); // Send Plugin Infos to Task Planer
        
}
    </pre>
    </blockquote>
    
    <blockquote>
    <h4>Add this into Method: SetPluginVariable()</h4>
    <pre>
        if (Regex.Match(strVariable, @""ExtraTaskPlaner_Callback"").Success) 
        {
            ExtraTaskPlaner_Callback(strValue);
        }

        if (Regex.Match(strVariable, @""Sample Variable"").Success) 
        {
            sampleVar = strValue;
        }

    </pre>
    </blockquote>

    <blockquote>
    <h4>Add this into Method: OnPluginLoaded()</h4>
    <pre>
     Thread startup_sleep = new Thread(new ThreadStart(delegate()
    {
        Thread.Sleep(2000);
        if (IsExtraTaskPlanerInstalled())
        {
            do
            {
                SendTaskPlanerInfo();
                Thread.Sleep(2000);
            }
            while (!isRegisteredInTaskPlaner);
        }
    }));
    startup_sleep.Start();   

    </pre>
    </blockquote>

       
    ";



}

return PluginDescription;
}




public List<CPluginVariable> GetDisplayPluginVariables() {
    
    List<CPluginVariable> lstReturn = new List<CPluginVariable>();
   try
    {

        

        UpdateEnums();

        lstReturn.Add(new CPluginVariable("Z_ Special Settings|Debug level", typeof(int), DebugLevel));
        lstReturn.Add(new CPluginVariable("Z_ Special Settings|Advanced Mode", typeof(enumBoolOnOff), AdvancedMode));
        lstReturn.Add(new CPluginVariable("Z_ Special Settings|Plugin Developer Info", typeof(enumBoolYesNo), PluginDeveloper));
       

        lstReturn.Add(new CPluginVariable("1. Taskmanager|Add Task", "enum.TaskTypes(...|Player Count|Time|Day Time|Date Time)", ""));
        lstReturn.Add(new CPluginVariable("1. Taskmanager|Remove Task", typeof(string), ""));
       //lstReturn.Add(new CPluginVariable("1. Taskmanager|Remove Task", enumRemoveTaskList, ""));

        lstReturn.Add(new CPluginVariable("1. Taskmanager|Current Task: ", typeof(string), currentState, true));
       



        foreach (KeyValuePair<int, Task> entry in Taskmanager)
        {
            bool defLock = false;
            if (entry.Value.Type == "DEFAULT") defLock = true; // Lock Variable in Default Task

            string tmpMenu = "Task " + (entry.Key).ToString() + " " + entry.Value.Name + "|" + "Task_" + (entry.Key).ToString() + "_";
            lstReturn.Add(new CPluginVariable(tmpMenu + "Enable", typeof(enumBoolYesNo), entry.Value.isEnabled, defLock));
            lstReturn.Add(new CPluginVariable(tmpMenu + "Type", typeof(string), entry.Value.Type, true));
            lstReturn.Add(new CPluginVariable(tmpMenu + "Name", typeof(string), entry.Value.Name, defLock));
            lstReturn.Add(new CPluginVariable(tmpMenu + "Priority", typeof(int), entry.Value.prio, defLock));

            if (entry.Value.Type == "Player Count")
            {
                lstReturn.Add(new CPluginVariable(tmpMenu + "Minimum Player Count", typeof(int), entry.Value.minPlayers));
                lstReturn.Add(new CPluginVariable(tmpMenu + "Maximum Player Count", typeof(int), entry.Value.maxPlayers));
            }

            if (entry.Value.Type == "Date Time")
            {
                lstReturn.Add(new CPluginVariable(tmpMenu + "Date Start", typeof(string), entry.Value.DateStart));
                lstReturn.Add(new CPluginVariable(tmpMenu + "Date End", typeof(string), entry.Value.DateEnd));

            }

            if (entry.Value.Type == "Day Time")
            {
                lstReturn.Add(new CPluginVariable(tmpMenu + "Monday", typeof(enumBoolOnOff), ConvertToEnumBoolOnOff(entry.Value.DaysOfWeek.Contains("Monday"))));
                lstReturn.Add(new CPluginVariable(tmpMenu + "Tuesday", typeof(enumBoolOnOff), ConvertToEnumBoolOnOff(entry.Value.DaysOfWeek.Contains("Tuesday"))));
                lstReturn.Add(new CPluginVariable(tmpMenu + "Wednesday", typeof(enumBoolOnOff), ConvertToEnumBoolOnOff(entry.Value.DaysOfWeek.Contains("Wednesday"))));
                lstReturn.Add(new CPluginVariable(tmpMenu + "Thursday", typeof(enumBoolOnOff), ConvertToEnumBoolOnOff(entry.Value.DaysOfWeek.Contains("Thursday"))));
                lstReturn.Add(new CPluginVariable(tmpMenu + "Friday", typeof(enumBoolOnOff), ConvertToEnumBoolOnOff(entry.Value.DaysOfWeek.Contains("Friday"))));
                lstReturn.Add(new CPluginVariable(tmpMenu + "Saturday", typeof(enumBoolOnOff), ConvertToEnumBoolOnOff(entry.Value.DaysOfWeek.Contains("Saturday"))));
                lstReturn.Add(new CPluginVariable(tmpMenu + "Sunday", typeof(enumBoolOnOff), ConvertToEnumBoolOnOff(entry.Value.DaysOfWeek.Contains("Sunday"))));


            }

            
            if (entry.Value.Type == "Time" || entry.Value.Type == "Day Time" || entry.Value.Type == "Date Time")
            {
                lstReturn.Add(new CPluginVariable(tmpMenu + "Time Start", typeof(string), entry.Value.TimeStart));
                lstReturn.Add(new CPluginVariable(tmpMenu + "Time End", typeof(string), entry.Value.TimeEnd));

            }

            

            lstReturn.Add(new CPluginVariable(tmpMenu + "Add Plugin", enumPluginAdd, ""));

            if (entry.Value.PluginCommands.Count > 0)
            {
                foreach (KeyValuePair<string, string> tmpCommands in entry.Value.PluginCommands)
                {
                    
                    
                    
                    lstReturn.Add(new CPluginVariable(tmpMenu + tmpCommands.Key + "_Command", enumPluginCommands[tmpCommands.Key], tmpCommands.Value));
                    
                    
                    if (RegisteredPlugins[tmpCommands.Key].Variables.Count > 0)
                    {
                        lstReturn.Add(new CPluginVariable(tmpMenu + tmpCommands.Key + "_Add Variable", enumPluginVariables[tmpCommands.Key] , ""));


                        foreach (KeyValuePair<string, string> tmpVariables in entry.Value.PluginVariables)
                        {


                            {
                                lstReturn.Add(new CPluginVariable(tmpMenu + tmpVariables.Key, typeof(string), tmpVariables.Value));
                            }

                        }

                    }
                }
            }        




            

            //    //if (strValue.StartsWith(tmpPlugin.Key))
            //    //{
            //    //    string tmpValue = strValue.Replace(tmpPlugin.Key, "");
            //    //    tmpValue = tmpValue.Trim();

            //    //    tmp.PluginCommands.Add(tmpValue, "none");
            //    //    WritePluginConsole("[Action Add] Plugin Name : " + tmpPlugin.Key + " Command : " + tmpValue, "DEBUG", 6);
            //    //}
            //}


            //lstReturn.Add(new CPluginVariable(tmpMenu + "Action Remove", enumAction, ""));
            //foreach ();

    
             
             
             
             }




        // Taskmanager
        //public int ID;
        //public int prio;
        //public string Type; // Time, PlayerCount, Interval, WeekdayTime, DateTime
        //public string Name;


        //public int minPlayers;
        //public int maxPlayers;

        //public int Intervall;



        //public DateTime TimeStart;
        //public DateTime TimeEnd;

        //public DateTime DateStart;
        //public DateTime DateEnd;

        //public List<string> DaysOfWeek;


        //public Dictionary<string, string> PluginCommands;
        //public Dictionary<string, List<string>> PluginVariables;
        //public List<string> ConfigList;


        return lstReturn;
    }
   catch (Exception ex)
   {
       WritePluginConsole("^1^b[GetDisplayPluginVariables] returs an Error: ^0^n" + ex.ToString(), "ERROR", 4);
       return lstReturn;
   }
   
}

public List<CPluginVariable> GetPluginVariables() {
	return GetDisplayPluginVariables();
}

public void SetPluginVariable(string strVariable, string strValue) {
     
    
    
    if (Regex.Match(strVariable, @"RegisterPlugin").Success)
    {
        RegisterPlugin(strValue);
        return;
    }


    if (strVariable.Contains("|"))
    {
        string[] tmpVariable = strVariable.Split('|');
        strVariable = tmpVariable[1];
    }


    WritePluginConsole("^b" + strVariable + "^n ( " + strValue + " )", "VARIABLE", 10);
    if (!SavePluginVariables.ContainsKey(strVariable)) SavePluginVariables.Add(strVariable, strValue);


    if (Regex.Match(strVariable, @"Add Task").Success)
    {
        if (strValue != "..." && strValue != "")
        {
            CreateTask(strValue);
        }
    }

    if (Regex.Match(strVariable, @"Remove Task").Success)
    {
        if (Taskmanager.ContainsKey(Convert.ToInt32(strValue)) && Convert.ToInt32(strValue) > 0)
        {
            Taskmanager.Remove(Convert.ToInt32(strValue));
            SortTaskmanager();
        }

    }

    if (strVariable == "Debug level")
    {
        int tmp = 2;
        int.TryParse(strValue, out tmp);
        DebugLevel = tmp;
    }


    if (strVariable == "Advanced Mode")
    {

        if (strValue == "On") AdvancedMode = enumBoolOnOff.On;
        if (strValue == "Off") AdvancedMode = enumBoolOnOff.Off;
        
    }

    if (strVariable == "Plugin Developer Info")
    {

        if (strValue == "Yes") PluginDeveloper = enumBoolYesNo.Yes;
        if (strValue == "No") PluginDeveloper = enumBoolYesNo.No;

    }
    
    
    
    
    
    
    if (strVariable.StartsWith("Task_"))
    {
        string[] tmpVar = strVariable.Split('_');

        int ID = Convert.ToInt32(tmpVar[1]);

        string tmpVariable = strVariable.Replace("Task_" + tmpVar[1] + "_", "");

        //string tmpVariable = tmpVar[2];
        
        
        Task tmp = new Task();

        if (Taskmanager.ContainsKey(ID))
        {
            
            tmp = Taskmanager[ID];
        }
        else
        {
        
            tmp.ID = ID;
            tmp.DaysOfWeek = new List<string>();
            tmp.ConfigList = new List<string>();
            tmp.PluginCommands = new Dictionary<string, string>();
            tmp.PluginVariables = new Dictionary<string, string>();

            Taskmanager.Add(ID, tmp);
        }
        
        

        if (tmpVariable == "Name") tmp.Name = strValue;
        if (tmpVariable == "Priority") tmp.prio = Convert.ToInt32(strValue);
        if (tmpVariable == "Type") tmp.Type = strValue;
        if (tmpVariable == "Date Start") tmp.DateStart = strValue;
        if (tmpVariable == "Date End") tmp.DateEnd = strValue;
        if (tmpVariable == "Time Start") tmp.TimeStart = strValue;
        
        
        if (tmpVariable == "Time End")
        { 
            tmp.TimeEnd = strValue;
            if (strValue == "00:00") tmp.TimeEnd = "23:59";
           
        }
        
        
        if (tmpVariable == "Enable")
        {
            if (strValue == "Yes") tmp.isEnabled = enumBoolYesNo.Yes;
            if (strValue == "No") tmp.isEnabled = enumBoolYesNo.No;
        }
        
        if (tmpVariable == "Minimum Player Count")
        {
            if (Convert.ToInt32(strValue) < Taskmanager[tmp.ID].maxPlayers)
            {
                tmp.minPlayers = Convert.ToInt32(strValue);
            }
            else
            {
                tmp.minPlayers = Convert.ToInt32(strValue);
                tmp.maxPlayers = Convert.ToInt32(strValue) + 1;
                //WritePluginConsole("Maximum Player Count can not be smaller then Minimum Player Count", "WARN", 2);
            }
        }
        if (tmpVariable == "Maximum Player Count")
        {

            if (Convert.ToInt32(strValue) > Taskmanager[tmp.ID].minPlayers)
            {
                tmp.maxPlayers = Convert.ToInt32(strValue);
            }
            else
            {
                tmp.minPlayers = 0;
                tmp.maxPlayers = Convert.ToInt32(strValue);
                //WritePluginConsole("Maximum Player Count can not be smaller then Minimum Player Count", "WARN", 2);
            }

        }

        if (tmpVariable == "Monday" || tmpVariable == "Tuesday" || tmpVariable == "Wednesday" || tmpVariable == "Thursday" || tmpVariable == "Friday" || tmpVariable == "Saturday" || tmpVariable == "Sunday")
        {
            if (strValue == "On" && !tmp.DaysOfWeek.Contains(tmpVariable)) tmp.DaysOfWeek.Add(tmpVariable);
            if (strValue == "Off" && tmp.DaysOfWeek.Contains(tmpVariable)) tmp.DaysOfWeek.Remove(tmpVariable);

        }




        
        if (tmpVariable == "Add Plugin")
        {
            foreach (KeyValuePair<string, PluginInfo> tmpPlugin in RegisteredPlugins)
            {
                if (strValue.StartsWith(tmpPlugin.Key) && !tmp.PluginCommands.ContainsKey(tmpPlugin.Key))
                {
                    //string tmpValue = strValue.Replace(tmpPlugin.Key, "");
                    //tmpValue = tmpValue.Trim();

                    tmp.PluginCommands.Add(tmpPlugin.Key, "none");

                    WritePluginConsole("[Plugin Add] Add Plugin : " + tmpPlugin.Key, "DEBUG", 6);
                }
            }
            
        }

        if (tmpVariable.Contains("_Command"))
        {
                foreach (KeyValuePair<string, PluginInfo> tmpPlugin in RegisteredPlugins)
                {
                    if (tmpVariable.StartsWith(tmpPlugin.Key))
                    {
                        //string tmpValue = strVariable.Replace("_" + tmpPlugin.Key, "");
                        //tmpValue = tmpValue.Trim();

                        if (tmp.PluginCommands.ContainsKey(tmpPlugin.Key)) tmp.PluginCommands[tmpPlugin.Key] = strValue;
                        if (!tmp.PluginCommands.ContainsKey(tmpPlugin.Key)) tmp.PluginCommands.Add(tmpPlugin.Key, strValue);

                        WritePluginConsole("[Command Add] Plugin Name : " + tmpPlugin.Key + " Command : " + strValue, "DEBUG", 6);
                    }


                }
            
        }

        if (tmpVariable.Contains("_Add Variable"))
        {
            if (strValue != "..." && strValue != "")
            {
                foreach (KeyValuePair<string, PluginInfo> tmpPlugin in RegisteredPlugins)
                {
                    if (tmpVariable.StartsWith(tmpPlugin.Key) && !tmp.PluginVariables.ContainsKey(tmpPlugin.Key))
                    {
                        //string tmpValue = strValue.Replace(tmpPlugin.Key, "");
                        //tmpValue = tmpValue.Trim();

                        tmp.PluginVariables.Add(tmpPlugin.Key + "_Variable_" + strValue, "");

                        WritePluginConsole("[Plugin Add] Add Variable : " + tmpPlugin.Key + "_Variable_" + strValue, "DEBUG", 6);
                    }
                }
            }
        }




        if (tmpVariable.Contains("_Variable") )
        {
            foreach (KeyValuePair<string, PluginInfo> tmpPlugin in RegisteredPlugins)
            {
                if (tmpVariable.StartsWith(tmpPlugin.Key))
                {
                    //string tmpValue = strVariable.Replace("_" + tmpPlugin.Key, "");
                    //tmpValue = tmpValue.Trim();

                    WritePluginConsole("[EDIT VARIABLE] " + tmpVariable + " ( " + strValue + " )", "DEBUG", 6);
                    if (tmp.PluginVariables.ContainsKey(tmpVariable)) tmp.PluginVariables[tmpVariable] = strValue;
                    if (!tmp.PluginVariables.ContainsKey(tmpVariable)) tmp.PluginVariables.Add(tmpVariable, strValue);


                }


            }

        }






        //if (tmpVariable == "Action Add")
        //{
        //    foreach (KeyValuePair<string,PluginInfo> tmpPlugin in RegisteredPlugins)
        //    {
        //        if (strValue.StartsWith(tmpPlugin.Key))
        //        {
        //            string tmpValue = strValue.Replace(tmpPlugin.Key, "");
        //            tmpValue = tmpValue.Trim();

        //            tmp.PluginCommands.Add(tmpValue);
        //            WritePluginConsole("[Action Add] Plugin Name : " + tmpPlugin.Key + " Command : " + tmpValue, "DEBUG", 6);
        //        }


        //    }
            
            
            
            
        
        //}        
        Taskmanager[tmp.ID] = tmp;



    }

    
    
   
}


public void UpdateEnums()
{
//    enumAction = "enum.Action_" + random.Next(100000, 999999) + "(...";

    var random = new Random();
    
    enumPluginAdd = "enum.PluginAdd_" + random.Next(100000, 999999) + "(...";

    
    if (RegisteredPlugins.Count > 0) 
    {
        foreach (KeyValuePair<string, PluginInfo> Plugin in RegisteredPlugins)
        {
            enumPluginAdd += "|" + Plugin.Key;

        }
        enumPluginAdd += ")";
    }

   
    
    if (RegisteredPlugins.Count > 0)
    {
               
        foreach (KeyValuePair<string, PluginInfo> Plugin in RegisteredPlugins)
        {
            if (!enumPluginCommands.ContainsKey(Plugin.Value.PluginName)) enumPluginCommands.Add(Plugin.Value.PluginName, "");
            
            enumPluginCommands[Plugin.Value.PluginName] = "enum.PluginCommands_" + Plugin.Value.PluginName + "_" + random.Next(100000, 999999) + "(none|Enable|Disable";
            
            foreach (string tmpCommand in Plugin.Value.Commands)
            {
                enumPluginCommands[Plugin.Value.PluginName] += "|" + tmpCommand;
            }
            
            enumPluginCommands[Plugin.Value.PluginName] += ")";

        }
        
    }


    if (RegisteredPlugins.Count > 0)
    {

        foreach (KeyValuePair<string, PluginInfo> Plugin in RegisteredPlugins)
        {
            if (!enumPluginVariables.ContainsKey(Plugin.Value.PluginName)) enumPluginVariables.Add(Plugin.Value.PluginName, "");

            enumPluginVariables[Plugin.Value.PluginName] = "enum.PluginVariables_" + Plugin.Value.PluginName + "_" + random.Next(100000, 999999) + "(...";

            foreach (string tmpVar in Plugin.Value.Variables)
            {
                enumPluginVariables[Plugin.Value.PluginName] += "|" + tmpVar;
            }

            enumPluginVariables[Plugin.Value.PluginName] += ")";

        }

    }
    
    
    
    
    
    
    

    enumRemoveTaskList = "enum.RemoveTask_" + random.Next(100000, 999999) + "(...";

    foreach (KeyValuePair<int, Task> entry in Taskmanager)
    {
        enumRemoveTaskList += "|" + entry.Key.ToString();
    }
    enumRemoveTaskList += ")";



    //if (RegisteredPlugins.Count > 0)
    //{
    //    foreach (KeyValuePair<string, PluginInfo> Plugin in RegisteredPlugins)
    //    {
    //        string tmpPluginHeader = "|" + Plugin.Key + " ";


    //        enumAction += tmpPluginHeader + "Enable";
    //        enumAction += tmpPluginHeader + "Disable";
    //        //if (Plugin.Value.Variables.Count > 0) enumAction += tmpPluginHeader + "Set Variable";



    //        //if (Plugin.Value.Commands.Count > 0)
    //        //{
    //        //    foreach (string command in Plugin.Value.Commands)
    //        //    {
    //        //        enumAction += tmpPluginHeader + command;
    //        //    }
    //        //}




    //        //if (Plugin.Value.Variables.Count > 0)
    //        //{
    //        //    foreach (string variable in Plugin.Value.Variables)
    //        //    {
    //        //        PluginDescription += @"<b>Variable:</b> " + variable + @"<br/>";
    //        //    }
    //        //}


    //    }
    //    enumAction += tmpPluginHeader + ")";
    //}




}

public enumBoolOnOff ConvertToEnumBoolOnOff(object rbool)
{
    bool truefalse = Convert.ToBoolean(rbool);

    if (truefalse) return enumBoolOnOff.On;
    return enumBoolOnOff.Off;
}


public void RegisterPlugin(params String[] commands) // Recive Infos from other Plugins
{
    if (commands.Length < 1)
    {
        WritePluginConsole("RegisterPlugin failed. No commands given.", "DEBUG", 3);
        return;
    }

    WritePluginConsole("RegisterPlugin recived " + commands.Length + " Datasets", "DEBUG", 6);
      

    new Thread(new ParameterizedThreadStart(DecodeRecivedPluginInfos)).Start(commands[0]);
}

private void CreateTask(string Type)
{
    Task tmp = new Task();
    tmp.ID = Taskmanager.Count;
    tmp.Type = Type;
    tmp.prio = 1;
    if (Type == "DEFAULT")
    {
        tmp.Name = "Default Configuration";
        tmp.prio = 0;

    }

    tmp.DateStart = DateTime.Today.ToShortDateString();
    tmp.DateEnd = DateTime.Today.ToShortDateString();
    tmp.TimeStart = "00:00";
    tmp.TimeEnd = "00:00";
    tmp.DaysOfWeek = new List<string>();
    tmp.ConfigList = new List<string>();
    tmp.PluginCommands = new Dictionary<string, string>();
    tmp.PluginVariables = new Dictionary<string, string>();

    Taskmanager.Add(tmp.ID, tmp);
}

private void CheckTask()
{
    Dictionary<int, Task> InRangeTasks = new Dictionary<int, Task>();

    foreach (KeyValuePair<int, Task> pair in Taskmanager)
    {
        Task checkTask = pair.Value;

        if (checkTask.isEnabled == enumBoolYesNo.Yes) // Check if Task is Enabled
        {

            switch (checkTask.Type)
            {
                case "Player Count": // Player Count Based Task
                    {
                        //WritePluginConsole("Check Task Player Count: " + checkTask.ID + " " + checkTask.Name + " Min Players: " + checkTask.minPlayers + " Max Players: " + checkTask.maxPlayers + " Current Players: " + playerCount, "DEBUG", 6);
                        if (checkTask.minPlayers <= playerCount && checkTask.maxPlayers >= playerCount) // Check if Task PlayerCount is in Range
                        {
                            InRangeTasks.Add(InRangeTasks.Count, checkTask);
                        }
                        break;
                    }
                case "Time":
                    {
                       
                        if (Convert.ToDateTime(checkTask.TimeStart) <= DateTime.Now && Convert.ToDateTime(checkTask.TimeEnd) >= DateTime.Now)
                        {
                            InRangeTasks.Add(InRangeTasks.Count, checkTask);                        
                        }
                        
                        
                        break;
                    }
                case "Day Time":
                    {
                        DayOfWeek today = DateTime.Today.DayOfWeek;
                        if (checkTask.DaysOfWeek.Contains(today.ToString()) && Convert.ToDateTime(checkTask.TimeStart) <= DateTime.Now && Convert.ToDateTime(checkTask.TimeEnd) >= DateTime.Now)
                        {
                            InRangeTasks.Add(InRangeTasks.Count, checkTask);
                        }
                        break;
                    }
                case "Date Time":
                    {
                        if (Convert.ToDateTime(checkTask.DateStart + " " + checkTask.TimeStart) <= DateTime.Now && Convert.ToDateTime(checkTask.DateEnd + " " + checkTask.TimeEnd) >= DateTime.Now)
                        {
                            InRangeTasks.Add(InRangeTasks.Count, checkTask);
                        }
                        
                        break;
                    }
                case "Interval":
                    {
                        break;
                    }
            }
        }
    }

    if (InRangeTasks.Count > 0)
    {
        int highPrioKey = 0;
        int highPrioValue = 0;

        foreach (KeyValuePair<int, Task> pair in InRangeTasks)
        {
            Task checkTask = pair.Value;

            if (checkTask.prio > highPrioValue)
            {
                highPrioKey = pair.Key;
                highPrioValue = checkTask.prio;
            }

        }
        RunTask(InRangeTasks[highPrioKey]);
        
    }
    else // No Task Match
    {
            RunTask(Taskmanager[0]);
    }
    





}

private void RunTask(Task runTask)
{
    WritePluginConsole("Got Task: " + runTask.ID.ToString() + " " + runTask.Name, "DEBUG", 6);

    if ((runTask.ID != currentTask.ID) && !startupLock) // Check if Task is loaded already abd Startup Lock is not set
    {
        currentTask = runTask;
        WritePluginConsole("Activate Task: " + runTask.ID.ToString() + " " + runTask.Name, "INFO", 2);
        currentState = runTask.ID.ToString() + " " + runTask.Name; // Show Current Task in Plugin Config
        this.ExecuteCommand("procon.protected.plugins.setVariable", "ExtraTaskPlaner", "UpdateVars", ""); // Update Variables Window
        foreach (KeyValuePair<string, string> PluginCommand in runTask.PluginCommands) // Send Commands to Plugins
        {
            if (PluginCommand.Value == "Enable" || PluginCommand.Value == "Disable")
            {
                WritePluginConsole("Send " + PluginCommand.Value + " Command to " + PluginCommand.Key, "DEBUG", 6);

                if (PluginCommand.Value == "Enable") this.ExecuteCommand("procon.protected.plugins.enable", RegisteredPlugins[PluginCommand.Key].PluginClassName, "true"); // Send Enable Command
                if (PluginCommand.Value == "Disable") this.ExecuteCommand("procon.protected.plugins.enable", RegisteredPlugins[PluginCommand.Key].PluginClassName, "false"); // Send Disbale Command
            }
            else
            {
                if (PluginCommand.Value != "none")
                {
                    WritePluginConsole("Send " + PluginCommand.Value + " Command to " + PluginCommand.Key, "DEBUG", 6);
                    this.ExecuteCommand("procon.protected.plugins.setVariable", RegisteredPlugins[PluginCommand.Key].PluginClassName, "ExtraTaskPlaner_Callback", PluginCommand.Value); // Send Command to Plugin
                }
            }
            
        }

        foreach (KeyValuePair<string, string> PluginVariable in runTask.PluginVariables) // Send Variables to Plugins
        {
            string tmpVariable = PluginVariable.Key;
            string[] tmpVar = tmpVariable.Split('_');
            string PluginName = tmpVar[0];
            string PluginVar = tmpVariable.Replace(tmpVar[0] + "_" + tmpVar[1] + "_", "");
            

            WritePluginConsole("Send Variable " + PluginVar + "(" + PluginVariable.Value + ") to " + PluginName , "DEBUG", 6);

            this.ExecuteCommand("procon.protected.plugins.setVariable", RegisteredPlugins[PluginName].PluginClassName, PluginVar, PluginVariable.Value); // Send Variable to Plugin

        }




    
    }


}



private void SortTaskmanager()
{
    Dictionary<int,Task> tmpTaskmanager = new Dictionary<int, Task>();

    foreach (KeyValuePair<int, Task> strTask in Taskmanager)
    {
        Task tmpTask = new Task();
        tmpTask = strTask.Value;
        tmpTask.ID = tmpTaskmanager.Count;

        tmpTaskmanager.Add(tmpTaskmanager.Count,tmpTask);

    }

    Taskmanager = tmpTaskmanager;
}


private void DecodeRecivedPluginInfos(Object jsonRequest)
{
    WritePluginConsole("Start decoding of recived plugin info", "DEBUG", 6);

    Thread.CurrentThread.Name = "DecodeRecivedPluginInfos";

    Hashtable parsedRequest = (Hashtable)JSON.JsonDecode((String)jsonRequest);

    PluginInfo plugin = new PluginInfo();
        
    plugin.PluginName = String.Empty;
    plugin.PluginClassName = String.Empty;
    plugin.PluginVersion = String.Empty;
    plugin.Commands = new List<string>();
    plugin.Variables = new List<string>();
    

    //public struct PluginInfo

    //public string PluginName;
    //public string PluginClassName;
    //public string PluginVersion;
    //public List<string> Commands;
    //public List<string> Variables;


    
    
    if (!parsedRequest.ContainsKey("PluginName"))
    {
        WritePluginConsole("Parsed commands didn't contain a PluginName!","DEBUG",6);
        return;
    }
    else
    {
        plugin.PluginName = (String)parsedRequest["PluginName"];
    }

    if (!parsedRequest.ContainsKey("PluginVersion"))
    {
        WritePluginConsole("Parsed commands didn't contain a PluginVersion!", "DEBUG", 6);
        return;
    }
    else
    {
        plugin.PluginVersion = (String)parsedRequest["PluginVersion"];
    }

    if (!parsedRequest.ContainsKey("PluginClassname"))
    {
        WritePluginConsole("Parsed commands didn't contain a PluginClassname!", "DEBUG", 6);
        return;
    }
    else
    {
        plugin.PluginClassName = (String)parsedRequest["PluginClassname"];
    }


    if (!parsedRequest.ContainsKey("PluginCommands"))
    {
        WritePluginConsole("Parsed commands didn't contain a Commands list!", "DEBUG", 6);
    }
    else
    {

        plugin.Commands = new List<string>(CPluginVariable.DecodeStringArray((string)parsedRequest["PluginCommands"]));

    }


    if (!parsedRequest.ContainsKey("PluginVariables"))
    {
        WritePluginConsole("Parsed commands didn't contain a Variables list!", "DEBUG", 6);
        
    }
    else
    {
        plugin.Variables = new List<string>(CPluginVariable.DecodeStringArray((string)parsedRequest["PluginVariables"]));
    }



    
    

    try
    {

        if (plugin.PluginName != String.Empty)
        {
            this.ExecuteCommand("procon.protected.plugins.setVariable", plugin.PluginClassName, "ExtraTaskPlaner_Callback", "success"); // Recive was successfull

            if (!RegisteredPlugins.ContainsKey(plugin.PluginName))
            {
                RegisteredPlugins.Add(plugin.PluginName, plugin);
                WritePluginConsole("Added " + plugin.PluginName + " to Plugin Database", "DEBUG", 6);
            }

            if (RegisteredPlugins.ContainsKey(plugin.PluginName))
            {
                RegisteredPlugins[plugin.PluginName] = plugin;
                WritePluginConsole("Updated " + plugin.PluginName + " in Plugin Database", "DEBUG", 6);
            }


        }
        else
        {

        }

    }
    catch (Exception ex)
    {
        WritePluginConsole("^1^b[DecodeRecivedPluginInfos] returs an Error: ^0^n" + ex.ToString(), "ERROR", 4);
    }



}


private void ResendPluginConfig()
{
    //Thread.CurrentThread.Name = "ResendPluginConfig";
    
    do
    {
        Thread.Sleep(1000);
    } 
    while (RegisteredPlugins.Count < 1);

    
    foreach (KeyValuePair<string, string> Variable in SavePluginVariables)
    {
        this.ExecuteCommand("procon.protected.plugins.setVariable", "ExtraTaskPlaner" , Variable.Key, Variable.Value);
    }

    Thread.Sleep(2000);

    foreach (KeyValuePair<string, string> Variable in SavePluginVariables)
    {
        this.ExecuteCommand("procon.protected.plugins.setVariable", "ExtraTaskPlaner", Variable.Key, Variable.Value);
    }
    


}


private void StartupTimer()
{
    Thread.Sleep(10000);

    WritePluginConsole("Start my work", "DEBUG", 6);
    currentTask.ID = 1000;
    startupLock = false;

}










public void OnPluginLoaded(string strHostName, string strPort, string strPRoConVersion) {
	this.RegisterEvents(this.GetType().Name, "OnVersion", "OnServerInfo", "OnResponseError", "OnListPlayers", "OnPlayerJoin", "OnPlayerLeft", "OnPlayerKilled", "OnPlayerSpawned", "OnPlayerTeamChange", "OnGlobalChat", "OnTeamChat", "OnSquadChat", "OnRoundOverPlayers", "OnRoundOver", "OnRoundOverTeamScores", "OnLoadingLevel", "OnLevelStarted", "OnLevelLoaded");

    this.RegisterCommand(matchCommandLookupRequest);
    startupLock = true;

    new Thread(new ThreadStart(ResendPluginConfig)).Start();
                     
}

public void OnPluginEnable() {
	IsEnabled = true;
	WritePluginConsole("Enabled!","INFO",2);
    new Thread(new ThreadStart(StartupTimer)).Start();
}

public void OnPluginDisable() {
	IsEnabled = false;
    WritePluginConsole("Disabled!", "INFO", 2);
}


public override void OnVersion(string serverType, string version) { }

public override void OnServerInfo(CServerInfo serverInfo)
{
    playerCount = serverInfo.PlayerCount;
    maxPlayerCount = serverInfo.MaxPlayerCount;
    ServerUptime = serverInfo.ServerUptime;


    CheckTask(); // Check if any Task Match
}

public override void OnResponseError(List<string> requestWords, string error) { }

public override void OnListPlayers(List<CPlayerInfo> players, CPlayerSubset subset) {
}

public override void OnPlayerJoin(string soldierName) {
}

public override void OnPlayerLeft(CPlayerInfo playerInfo) {
}

public override void OnPlayerKilled(Kill kKillerVictimDetails) { }

public override void OnPlayerSpawned(string soldierName, Inventory spawnedInventory) { }

public override void OnPlayerTeamChange(string soldierName, int teamId, int squadId) { }

public override void OnGlobalChat(string speaker, string message) { }

public override void OnTeamChat(string speaker, string message, int teamId) { }

public override void OnSquadChat(string speaker, string message, int teamId, int squadId) { }

public override void OnRoundOverPlayers(List<CPlayerInfo> players) { }

public override void OnRoundOverTeamScores(List<TeamScore> teamScores) { }

public override void OnRoundOver(int winningTeamId) { }

public override void OnLoadingLevel(string mapFileName, int roundsPlayed, int roundsTotal) { }

public override void OnLevelStarted() { }

public override void OnLevelLoaded(string mapFileName, string Gamemode, int roundsPlayed, int roundsTotal) { } // BF3


} // end ExtraTaskPlaner

} // end namespace PRoConEvents


public struct PluginInfo
{
    public string PluginName;
    public string PluginClassName;
    public string PluginVersion;
    public List<string> Commands;
    public List<string> Variables;
}


public struct Task
{
    public int ID;
    public int prio;
    public int minPlayers;
    public int maxPlayers;
    public int Intervall;
    public enumBoolYesNo isEnabled;
    public string Name;
    public string Type; // Time, PlayerCount, Interval, WeekdayTime, DateTime
    public string TimeStart;
    public string TimeEnd;
    public string DateStart;
    public string DateEnd;
    public List<string> DaysOfWeek;
    public List<string> ConfigList;
    public Dictionary<string, string> PluginCommands;
    public Dictionary<string, string> PluginVariables; // KEy: Task_x_Plugin Name_Variable_Variable Name   // Value: Value
       
}

