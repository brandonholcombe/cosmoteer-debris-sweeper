using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Cosmoteer.Game;
using Cosmoteer.Ships;
using Halfling;
using Halfling.Application;
using Halfling.Input;

[assembly: IgnoresAccessChecksTo("Cosmoteer")]
[assembly: IgnoresAccessChecksTo("HalflingCore")]

namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class IgnoresAccessChecksToAttribute : Attribute
    {
        public IgnoresAccessChecksToAttribute(string assemblyName)
        {
            AssemblyName = assemblyName;
        }

        public string AssemblyName { get; }
    }
}

/*
 * Debris Sweeper Hotkey (EnhancedModLoader C# mod)
 *
 *   F9        -> delete all junk (destroyed-ship debris) in the current system
 *   Ctrl+F9   -> delete all junk AND all loose resource nuggets
 *
 * Unlike passive decay, this only fires when you press the key, so pre-placed
 * salvage sites (ship graveyards, storage pods, abandoned ships — which career
 * mode spawns with the same "junk" allegiance as battle debris) are safe until
 * YOU decide the system is done being looted.
 *
 * Junk allegiance is -3 in the game data (see doodad rules: "Allegiance = -3 // Junk").
 * Member access on uncertain internals goes through small reflection helpers so
 * minor API renames between game versions degrade gracefully instead of crashing.
 */

namespace DebrisSweeperHotkey
{
    public class Main
    {
        private const ViKey ClearKey = ViKey.F9;
        private const int JunkAllegiance = -3;

        // Flip to true to get a popup with the sweep count (useful for first-run testing).
        private const bool ShowSweepReport = true;

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int MessageBox(int hWnd, string text, string caption, uint type);

        private static Keyboard? keyboard;

        [UnmanagedCallersOnly]
        public static void InitializePatches()
        {
            keyboard = Halfling.App.Keyboard;
            App.Director.FrameEnded += Worker;
        }

        private static void Worker(object? sender, EventArgs e)
        {
            if (keyboard == null)
                return;

            GameRoot? game = App.Director.States.OfType<GameRoot>().FirstOrDefault();
            if (game?.Sim == null)
                return;

            try
            {
                if (keyboard.HotkeyPressed(ViKey.PlatformCmdCtrl, ClearKey, true))
                    Sweep(game, alsoNuggets: true);
                else if (keyboard.HotkeyPressed(ClearKey, true))
                    Sweep(game, alsoNuggets: false);
            }
            catch (Exception ex)
            {
                MessageBox(0, ex.ToString(), "Debris Sweeper error", 0);
            }
        }

        private static void Sweep(GameRoot game, bool alsoNuggets)
        {
            object sim = game.Sim;

            // Snapshot before deleting — we must not mutate the collection mid-iteration.
            List<Ship> junk = EnumerateShips(sim).Where(IsJunk).ToList();
            int removed = 0;
            foreach (Ship ship in junk)
            {
                if (RemoveShip(sim, ship))
                    removed++;
            }

            int nuggets = 0;
            if (alsoNuggets)
                nuggets = ClearNuggets(sim);

            if (ShowSweepReport)
                MessageBox(0, $"Removed {removed} junk chunk(s)" +
                              (alsoNuggets ? $" and {nuggets} loose resource(s)." : "."),
                           "Debris Sweeper", 0);
        }

        // ---- Ships ----

        private static IEnumerable<Ship> EnumerateShips(object sim)
        {
            object? manager = GetMember(sim, "Ships");
            if (manager == null)
                return Enumerable.Empty<Ship>();

            if (manager is IEnumerable<Ship> typed)
                return typed.ToList();

            // ShipManager might wrap an inner collection under a few plausible names.
            foreach (string name in new[] { "Ships", "AllShips", "ShipList", "_ships" })
            {
                if (GetMember(manager, name) is IEnumerable inner)
                    return inner.OfType<Ship>().ToList();
            }

            if (manager is IEnumerable loose)
                return loose.OfType<Ship>().ToList();

            return Enumerable.Empty<Ship>();
        }

        private static bool IsJunk(Ship ship)
        {
            // Preferred: the game's own junk flag, if one exists on Ship.
            object? flag = GetMember(ship, "IsJunk");
            if (flag is bool b)
                return b;

            // Fallback: compare allegiance to the junk team (-3).
            object? allegiance =
                GetMember(GetMember(ship, "Metadata"), "Allegiance") ??
                GetMember(ship, "Allegiance");

            if (allegiance != null)
            {
                try { return Convert.ToInt32(allegiance) == JunkAllegiance; }
                catch { /* not numeric/enum — fall through */ }
            }

            return false;
        }

        private static bool RemoveShip(object sim, Ship ship)
        {
            if (InvokeFirst(ship, new[] { "Remove", "Destroy", "Despawn" }))
                return true;

            // Fall back to the manager's Remove(ship).
            object? manager = GetMember(sim, "Ships");
            return manager != null && InvokeFirst(manager, new[] { "Remove", "RemoveShip", "Destroy" }, ship);
        }

        // ---- Loose resource nuggets ----

        private static int ClearNuggets(object sim)
        {
            object? manager = GetMember(sim, "Nuggets");
            if (manager == null)
                return 0;

            IEnumerable? items = manager as IEnumerable;
            if (items == null)
            {
                foreach (string name in new[] { "Nuggets", "AllNuggets", "_nuggets" })
                {
                    if (GetMember(manager, name) is IEnumerable inner)
                    {
                        items = inner;
                        break;
                    }
                }
            }
            if (items == null)
                return 0;

            List<object> snapshot = items.OfType<object>().ToList();
            int removed = 0;
            foreach (object nugget in snapshot)
            {
                if (InvokeFirst(nugget, new[] { "Remove", "Destroy", "Despawn" }) ||
                    InvokeFirst(manager, new[] { "Remove", "RemoveNugget", "Destroy" }, nugget))
                    removed++;
            }
            return removed;
        }

        // ---- Reflection helpers ----

        private const BindingFlags AnyInstance =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        private static object? GetMember(object? obj, string name)
        {
            if (obj == null)
                return null;
            Type t = obj.GetType();
            PropertyInfo? prop = t.GetProperty(name, AnyInstance);
            if (prop != null && prop.GetIndexParameters().Length == 0)
                return prop.GetValue(obj);
            FieldInfo? field = t.GetField(name, AnyInstance);
            return field?.GetValue(obj);
        }

        private static bool InvokeFirst(object target, string[] methodNames, object? arg = null)
        {
            Type[] signature = arg == null ? Type.EmptyTypes : new[] { arg.GetType() };
            foreach (string name in methodNames)
            {
                MethodInfo? method = arg == null
                    ? target.GetType().GetMethod(name, AnyInstance, signature)
                    : FindMethodAccepting(target.GetType(), name, arg.GetType());
                if (method == null)
                    continue;
                try
                {
                    method.Invoke(target, arg == null ? null : new[] { arg });
                    return true;
                }
                catch { /* try next candidate */ }
            }
            return false;
        }

        private static MethodInfo? FindMethodAccepting(Type type, string name, Type argType)
        {
            return type.GetMethods(AnyInstance).FirstOrDefault(m =>
            {
                if (m.Name != name)
                    return false;
                ParameterInfo[] ps = m.GetParameters();
                return ps.Length == 1 && ps[0].ParameterType.IsAssignableFrom(argType);
            });
        }
    }
}
