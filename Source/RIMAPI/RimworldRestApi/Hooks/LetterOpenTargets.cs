using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RimworldRestApi.Hooks
{
    // Which OpenLetter implementations a letter hook has to patch.
    //
    // The list is found by walking the game's types, not by naming them: a
    // name in a hand-kept list goes stale without a compile error, and the
    // first version named Verse.LetterWithTimeout, which declares no
    // OpenLetter at all, so Harmony refused that patch on every game start
    // (ticket #3318). Walking the hierarchy also stops one letter type from
    // being patched twice through its subclasses, which would mark a letter
    // seen once per patch.
    public static class LetterOpenTargets
    {
        public const string MethodName = "OpenLetter";

        // Scan returns the implemented OpenLetter of every concrete subclass of
        // letterType, each method once, plus one problem line per type it could
        // not resolve. An abstract type contributes nothing: it declares no
        // implementation to patch, and its concrete subclasses are in the list
        // on their own.
        public static LetterOpenScan Scan(Type letterType, IEnumerable<Type> types)
        {
            var targets = new List<MethodInfo>();
            var problems = new List<string>();
            if (letterType == null)
            {
                problems.Add("the game's letter type was not found");
                return new LetterOpenScan(targets, problems);
            }
            if (types == null)
            {
                problems.Add("no types to scan");
                return new LetterOpenScan(targets, problems);
            }

            var seen = new HashSet<MethodInfo>();
            foreach (var type in types)
            {
                if (type == null || type == letterType || !letterType.IsAssignableFrom(type))
                    continue;
                if (type.IsAbstract)
                    continue;
                var method = Implementation(type, letterType);
                if (method == null)
                {
                    problems.Add(type.FullName + " is a letter but implements no " + MethodName);
                    continue;
                }
                if (seen.Add(method))
                    targets.Add(method);
            }
            return new LetterOpenScan(targets, problems);
        }

        // ScanLoadedAssemblies is Scan over everything the game has loaded, so a
        // letter type the game adds in an update is covered without an edit
        // here. An assembly that will not list its types (a missing dependency)
        // costs one problem line, not the whole hook.
        public static LetterOpenScan ScanLoadedAssemblies(Type letterType)
        {
            var types = new List<Type>();
            var problems = new List<string>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var name = assembly.GetName().Name;
                try
                {
                    types.AddRange(assembly.GetTypes());
                }
                catch (ReflectionTypeLoadException ex)
                {
                    // The types that did load are the ones that matter; the rest
                    // belong to a dependency this host does not have.
                    types.AddRange(ex.Types.Where(t => t != null));
                }
                catch (Exception ex)
                {
                    problems.Add("could not read the types of " + name + ": " + ex.Message);
                }
            }
            var scan = Scan(letterType, types);
            problems.AddRange(scan.Problems);
            return new LetterOpenScan(scan.Targets, problems);
        }

        // Implementation walks up from the concrete type to the letter type and
        // returns the first OpenLetter that has a body: the one the game
        // actually runs for this type.
        private static MethodInfo Implementation(Type type, Type letterType)
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic |
                                       BindingFlags.Instance | BindingFlags.DeclaredOnly;
            for (var current = type; current != null && current != letterType; current = current.BaseType)
            {
                var method = current.GetMethod(MethodName, flags);
                if (method != null && !method.IsAbstract)
                    return method;
            }
            return null;
        }
    }

    // What a scan found: the methods to patch and what it could not resolve.
    public class LetterOpenScan
    {
        public LetterOpenScan(IReadOnlyList<MethodInfo> targets, IReadOnlyList<string> problems)
        {
            Targets = targets;
            Problems = problems;
        }

        public IReadOnlyList<MethodInfo> Targets { get; }

        public IReadOnlyList<string> Problems { get; }
    }
}
