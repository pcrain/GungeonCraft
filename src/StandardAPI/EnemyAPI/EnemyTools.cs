using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace EnemyAPI
{
    public static class EnemyTools
    {
        public static ReadOnlyCollection<OverrideBehavior> overrideBehaviors = null;
        static bool hasInit = false;

        public static void Init()
        {
            try
            {
                List<OverrideBehavior> obs = new List<OverrideBehavior>();
                foreach (Type type in
                Assembly.GetAssembly(typeof(OverrideBehavior)).GetTypes()
                .Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(typeof(OverrideBehavior))))
                {
                    obs.Add((OverrideBehavior)Activator.CreateInstance(type));
                }
                overrideBehaviors = new ReadOnlyCollection<OverrideBehavior>(obs);
                hasInit = true;
            }
            catch (Exception e)
            {
                ETGModConsole.Log("Failed to init EnemyAPI! Please contact spcreat!\n\n" + e);
            }
        }

        public static void ManualAddOB(Type ob)
        {
            if (ob.IsClass && !ob.IsAbstract && ob.IsSubclassOf(typeof(OverrideBehavior)))
            {
                var l = new List<OverrideBehavior>(overrideBehaviors);
                l.Add((OverrideBehavior)Activator.CreateInstance(ob));
                overrideBehaviors = new ReadOnlyCollection<OverrideBehavior>(l);
            }
        }
    
        public static void DebugInformation(BehaviorSpeculator behavior, string path = "")
        {
            List<string> logs = new List<string>();

            logs.Add("Enemy report for enemy '" + behavior.aiActor.ActorName + "' with ID " + behavior.aiActor.EnemyGuid + ":");
            logs.Add("");

            logs.Add("--- Beginning behavior report");
            foreach (var b in behavior.AttackBehaviors)
            {
                if (b is AttackBehaviorGroup)
                {
                    logs.Add("Note: This actor has an AttackBehaviorGroup. The nicknames and probabilities are as follows:");
                    foreach (var be in (b as AttackBehaviorGroup).AttackBehaviors)
                    {
                        logs.Add(" - " + be.NickName + " | " + be.Probability);
                    }
                    foreach (var be in (b as AttackBehaviorGroup).AttackBehaviors)
                    {
                        logs.Add(ReturnPropertiesAndFields(be.Behavior, "Logging AttackBehaviorGroup behavior " + be.Behavior.GetType().Name + " with nickname " + be.NickName + " and probability " + be.Probability));
                    }
                }
                else
                {
                    logs.Add(ReturnPropertiesAndFields(b, "Logging attack behavior " + b.GetType().Name));
                }
            }
            logs.Add("-----");
            foreach (var b in behavior.MovementBehaviors)
            {
                logs.Add(ReturnPropertiesAndFields(b, "Logging movement behavior " + b.GetType().Name));
            }
            logs.Add("-----");
            foreach (var b in behavior.OtherBehaviors)
            {
                logs.Add(ReturnPropertiesAndFields(b, "Logging other behavior " + b.GetType().Name));
            }
            logs.Add("-----");
            foreach (var b in behavior.OverrideBehaviors)
            {
                logs.Add(ReturnPropertiesAndFields(b, "Logging override behavior " + b.GetType().Name));
            }
            logs.Add("-----");
            foreach (var b in behavior.TargetBehaviors)
            {
                logs.Add(ReturnPropertiesAndFields(b, "Logging target behavior " + b.GetType().Name));
            }
            logs.Add("--- End of behavior report");
            logs.Add("");

            logs.Add("Components attached to the actor object are listed below.");
            foreach (var c in behavior.aiActor.gameObject.GetComponents(typeof(object)))
            {
                logs.Add(c.GetType().Name);
            }

            logs.Add("");
            if (behavior.bulletBank)
            {
                logs.Add("--- Beginning bullet bank report");

                foreach (var b in behavior.bulletBank.Bullets)
                {
                    logs.Add(ReturnPropertiesAndFields(b, "Logging bullet " + b.Name));
                }
                logs.Add("--- End of bullet bank report");
            }
            else
            {
                logs.Add("--- Actor does not have a bullet bank.");
            }

            var retstr = string.Join("\n", logs.ToArray());
            if (string.IsNullOrEmpty(path))
            {
                ETGModConsole.Log(retstr);
            }
            else
            {
                File.WriteAllText(path, retstr);
            }
        }

        public static void DebugInformationNoAIActor(BehaviorSpeculator behavior, string path = "")
        {
            List<string> logs = new List<string>();
            
            logs.Add("Enemy report");
            logs.Add("");
            
            logs.Add("--- Beginning behavior report");
            foreach (var b in behavior.AttackBehaviors)
            {
                if (b is AttackBehaviorGroup)
                {
                    logs.Add("Note: This actor has an AttackBehaviorGroup. The nicknames and probabilities are as follows:");
                    foreach (var be in (b as AttackBehaviorGroup).AttackBehaviors)
                    {
                        logs.Add(" - " + be.NickName + " | " + be.Probability);
                    }
                    foreach (var be in (b as AttackBehaviorGroup).AttackBehaviors)
                    {
                        logs.Add(ReturnPropertiesAndFields(be.Behavior, "Logging AttackBehaviorGroup behavior " + be.Behavior.GetType().Name + " with nickname " + be.NickName + " and probability " + be.Probability));
                    }
                }
                else
                {
                    logs.Add(ReturnPropertiesAndFields(b, "Logging attack behavior " + b.GetType().Name));
                }
            }
            logs.Add("-----");
            foreach (var b in behavior.MovementBehaviors)
            {
                logs.Add(ReturnPropertiesAndFields(b, "Logging movement behavior " + b.GetType().Name));
            }
            logs.Add("-----");
            foreach (var b in behavior.OtherBehaviors)
            {
                logs.Add(ReturnPropertiesAndFields(b, "Logging other behavior " + b.GetType().Name));
            }
            logs.Add("-----");
            foreach (var b in behavior.OverrideBehaviors)
            {
                logs.Add(ReturnPropertiesAndFields(b, "Logging override behavior " + b.GetType().Name));
            }
            logs.Add("-----");
            foreach (var b in behavior.TargetBehaviors)
            {
                logs.Add(ReturnPropertiesAndFields(b, "Logging target behavior " + b.GetType().Name));
            }
            logs.Add("--- End of behavior report");
            logs.Add("");
            
            logs.Add("Components attached to the object are listed below.");
            foreach (var c in behavior.gameObject.GetComponents(typeof(object)))
            {
                logs.Add(c.GetType().Name);
            }
            
            logs.Add("");
            if (behavior.bulletBank)
            {
                logs.Add("--- Beginning bullet bank report");

                foreach (var b in behavior.bulletBank.Bullets)
                {
                    logs.Add(ReturnPropertiesAndFields(b, "Logging bullet " + b.Name));
                }
                logs.Add("--- End of bullet bank report");
            }
            else
            {
                logs.Add("--- Actor does not have a bullet bank.");
            }
            
            var retstr = string.Join("\n", logs.ToArray());
            if (string.IsNullOrEmpty(path))
            {
                ETGModConsole.Log(retstr);
            }
            else
            {
                File.WriteAllText(path, retstr);
            }
        }

        public static void DebugBulletBank(AIBulletBank bank, string path = "")
        {
            List<string> logs = new List<string>();

            logs.Add("bullet bank report");
            logs.Add("");

            logs.Add("");
            if (bank)
            {
                logs.Add("--- Beginning bullet bank report");

                foreach (var b in bank.Bullets)
                {
                    logs.Add(ReturnPropertiesAndFields(b, "Logging bullet " + b.Name));
                }
                logs.Add("--- End of bullet bank report");
            }
            else
            {
                logs.Add("--- Actor does not have a bullet bank.");
            }

            var retstr = string.Join("\n", logs.ToArray());
            if (string.IsNullOrEmpty(path))
            {
                ETGModConsole.Log(retstr);
            }
            else
            {
                File.WriteAllText(path, retstr);
            }
        }

        public static string ReturnPropertiesAndFields<T>(T obj, string header = "")
        {
            string ret = "";
            ret += "\r\n" + (header);
            ret += "\r\n" + ("=======================");
            if (obj == null) { ret += "\r\n" + ("LogPropertiesAndFields: Null object"); return ret; }
            Type type = obj.GetType();
            ret += "\r\n" + ($"{typeof(T)} Fields: ");
            FieldInfo[] finfos = type.GetFields();
            foreach (var finfo in finfos)
            {
                try
                {
                    var value = finfo.GetValue(obj);
                    string valueString = value.ToString();
                    bool isArray = value.GetType().IsArray == true;
                    if (isArray)
                    {
                        var ar = (value as IEnumerable);
                        valueString = $"Array[]";
                        foreach (var subval in ar)
                        {
                            valueString += "\r\n\t\t" + subval.ToString();
                        }
                    }
                    else if (value is BulletScriptSelector)
                    {
                        valueString = (value as BulletScriptSelector).scriptTypeName;
                    }
                    ret += "\r\n" + ($"\t{finfo.Name}: {valueString}");
                }
                catch
                {
                    ret += "\r\n" + ($"\t{finfo.Name}: {finfo.GetValue(obj)}");
                }
            }

            return ret;
        }
    }
}
