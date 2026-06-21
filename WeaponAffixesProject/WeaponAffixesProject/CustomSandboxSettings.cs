using System;
using System.Linq;
using System.Reflection;

namespace WeaponAffixesProject
{
    internal static class CustomSandboxSettings
    {
        internal const string MaxAffixes = "MaxAffixes";
        internal const string AffixAbundance = "AffixAbundance";
        internal const string KillsToUpgrade = "KillsToUpgrade";

        private static bool registered;

        internal static void Register()
        {
            if (registered)
                return;

            registered = true;

            RegisterIntSetting(MaxAffixes, 5, 1, 12);
            RegisterIntSetting(AffixAbundance, 100, 0, 300);
            RegisterIntSetting(KillsToUpgrade, 100, 25, 500);
        }

        internal static int GetInt(string name, int defaultValue)
        {
            Register();

            object value;
            if (TryGetGamePref(name, out value) && TryConvertToInt(value, out int intValue))
                return intValue;

            return defaultValue;
        }

        private static void RegisterIntSetting(string name, int defaultValue, int minValue, int maxValue)
        {
            Type gamePrefsType = FindType("GamePrefs");
            if (gamePrefsType == null)
            {
                Log.Warning($"[RAM] Could not find GamePrefs while registering sandbox setting '{name}'.");
                return;
            }

            if (TryRegisterByKnownMethods(gamePrefsType, name, defaultValue, minValue, maxValue))
                return;

            EnsureDefaultValue(gamePrefsType, name, defaultValue);
        }

        private static bool TryRegisterByKnownMethods(Type gamePrefsType, string name, int defaultValue, int minValue, int maxValue)
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
            string[] registerMethodNames =
            {
                "RegisterCustomPref",
                "RegisterCustomGamePref",
                "RegisterCustomGamePreference",
                "RegisterCustomSandboxPref",
                "RegisterCustomSandboxSetting",
                "AddCustomPref",
                "AddCustomGamePref",
                "AddCustomGamePreference",
                "AddCustomSandboxPref",
                "AddCustomSandboxSetting"
            };

            foreach (string methodName in registerMethodNames)
            {
                foreach (MethodInfo method in gamePrefsType.GetMethods(flags).Where(method => method.Name == methodName))
                {
                    object[] args = BuildArguments(method, name, defaultValue, minValue, maxValue);
                    if (args == null)
                        continue;

                    try
                    {
                        method.Invoke(null, args);
                        Log.Out($"[RAM] Registered sandbox setting '{name}' through GamePrefs.{method.Name}.");
                        return true;
                    }
                    catch (TargetInvocationException e)
                    {
                        Log.Warning($"[RAM] GamePrefs.{method.Name} rejected sandbox setting '{name}': {e.InnerException?.Message ?? e.Message}");
                    }
                    catch (Exception e)
                    {
                        Log.Warning($"[RAM] Could not register sandbox setting '{name}' through GamePrefs.{method.Name}: {e.Message}");
                    }
                }
            }

            return false;
        }

        private static object[] BuildArguments(MethodInfo method, string name, int defaultValue, int minValue, int maxValue)
        {
            ParameterInfo[] parameters = method.GetParameters();
            object[] args = new object[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                Type parameterType = parameters[i].ParameterType;
                string parameterName = parameters[i].Name?.ToLowerInvariant() ?? string.Empty;

                if (parameterType == typeof(string))
                {
                    args[i] = parameterName.Contains("type") ? "int" : name;
                }
                else if (parameterType == typeof(int))
                {
                    if (parameterName.Contains("min")) args[i] = minValue;
                    else if (parameterName.Contains("max")) args[i] = maxValue;
                    else args[i] = defaultValue;
                }
                else if (parameterType == typeof(float))
                {
                    if (parameterName.Contains("min")) args[i] = (float)minValue;
                    else if (parameterName.Contains("max")) args[i] = (float)maxValue;
                    else args[i] = (float)defaultValue;
                }
                else if (parameterType == typeof(bool))
                {
                    args[i] = false;
                }
                else if (parameterType.IsEnum)
                {
                    object parsed;
                    if (TryParseEnum(parameterType, "Int", out parsed) || TryParseEnum(parameterType, "Integer", out parsed))
                        args[i] = parsed;
                    else
                        args[i] = Enum.GetValues(parameterType).GetValue(0);
                }
                else if (parameterType == typeof(object))
                {
                    args[i] = defaultValue;
                }
                else if (parameters[i].HasDefaultValue)
                {
                    args[i] = parameters[i].DefaultValue;
                }
                else
                {
                    return null;
                }
            }

            return args;
        }

        private static bool TryParseEnum(Type enumType, string value, out object parsed)
        {
            try
            {
                parsed = Enum.Parse(enumType, value, true);
                return true;
            }
            catch
            {
                parsed = null;
                return false;
            }
        }

        private static void EnsureDefaultValue(Type gamePrefsType, string name, int defaultValue)
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
            foreach (string methodName in new[] { "Set", "SetString", "SetObject" })
            {
                MethodInfo method = gamePrefsType.GetMethods(flags).FirstOrDefault(candidate => candidate.Name == methodName && candidate.GetParameters().Length >= 2 && candidate.GetParameters()[0].ParameterType == typeof(string));
                if (method == null)
                    continue;

                try
                {
                    ParameterInfo[] parameters = method.GetParameters();
                    object value = parameters[1].ParameterType == typeof(string) ? defaultValue.ToString() : (object)defaultValue;
                    method.Invoke(null, new[] { name, value });
                    Log.Out($"[RAM] Initialized fallback value for sandbox setting '{name}'.");
                    return;
                }
                catch (Exception e)
                {
                    Log.Warning($"[RAM] Could not initialize fallback value for sandbox setting '{name}': {e.Message}");
                }
            }
        }

        private static bool TryGetGamePref(string name, out object value)
        {
            value = null;
            Type gamePrefsType = FindType("GamePrefs");
            if (gamePrefsType == null)
                return false;

            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
            foreach (string methodName in new[] { "GetInt", "GetString", "GetObject", "Get" })
            {
                MethodInfo method = gamePrefsType.GetMethods(flags).FirstOrDefault(candidate => candidate.Name == methodName && candidate.GetParameters().Length >= 1 && candidate.GetParameters()[0].ParameterType == typeof(string));
                if (method == null)
                    continue;

                try
                {
                    value = method.Invoke(null, new object[] { name });
                    return value != null;
                }
                catch { }
            }

            return false;
        }

        private static bool TryConvertToInt(object value, out int intValue)
        {
            if (value is int i)
            {
                intValue = i;
                return true;
            }

            return int.TryParse(value?.ToString(), out intValue);
        }

        private static Type FindType(string typeName)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(typeName) ?? assembly.GetTypes().FirstOrDefault(candidate => candidate.Name == typeName);
                if (type != null)
                    return type;
            }

            return null;
        }
    }
}
