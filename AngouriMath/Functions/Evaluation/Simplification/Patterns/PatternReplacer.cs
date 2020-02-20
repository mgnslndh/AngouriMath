﻿using AngouriMath.Core;
using AngouriMath.Core.Exceptions;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("UnitTests")]

namespace AngouriMath
{
    public abstract partial class Entity
    {
        internal readonly EntType entType;

        internal enum EntType
        {
            NUMBER,
            FUNCTION,
            OPERATOR,
            VARIABLE,
            PATTERN,
            TENSOR
        }
        internal enum PatType
        {
            NONE,
            COMMON,
            NUMBER,
            VARIABLE,
            FUNCTION,
            OPERATOR
        }
        internal int PatternNumber { get; set; }
        internal PatType PatternType; // not a getter due to performance requirements
        internal static bool PatternMatches(Entity pattern, Entity tree)
        {
            if (!(pattern.PatternType == PatType.NUMBER && tree.entType == EntType.NUMBER ||
                   pattern.PatternType == PatType.FUNCTION && tree.entType == EntType.FUNCTION ||
                   pattern.PatternType == PatType.OPERATOR && tree.entType == EntType.OPERATOR ||
                   pattern.PatternType == PatType.VARIABLE && tree.entType == EntType.VARIABLE))
                return false;
            return string.IsNullOrEmpty(pattern.Name) || pattern.Name == tree.Name;
        }

        /// <summary>
        /// Checks if a pattern or pattern tree matches an expression.
        /// Important to keep all constants inside Num()
        /// </summary>
        /// <param name="tree"></param>
        /// <returns>
        /// Whether it fits or not
        /// </returns>
        internal bool Match(Entity tree)
        {
            if (this.PatternType == PatType.NONE)
                return this == tree;
            if (PatternType == PatType.COMMON)
                return (this as Pattern).EqFits(tree) != null;
            if (!PatternMatches(this, tree))
                return false;
            if (PatternType == PatType.FUNCTION && PatternNumber != -1)
                return (this as Pattern).EqFits(tree) != null;
            if (Children.Count != tree.Children.Count)
                return false;
            for (int i = 0; i < Children.Count; i++)
                if (!Children[i].Match(tree.Children[i]))
                    return false;
            return (this as Pattern).EqFits(tree) != null;
        }

        /// <summary>
        /// Finds the first occurance of a subtree that fits a pattern
        /// </summary>
        /// <param name="pattern"></param>
        /// <returns>
        /// Entity: first found subtree
        /// </returns>
        internal Entity FindPatternSubtree(Pattern pattern)
        {
            if (pattern.Match(this) && pattern.EqFits(this) != null)
                return this;
            Entity res;
            // foreach is slower than raw loop
            for (int i = 0; i < Children.Count; i++)
                if ((res = Children[i].FindPatternSubtree(pattern)) != null)
                    return res;
            return null;
        }

        /// <summary>
        /// Searchs for parent of the only argument
        /// </summary>
        /// <param name="kinder"></param>
        /// <returns></returns>
        internal Entity FindParent(Entity kinder)
        {
            foreach (var child in Children)
            {
                if ((object)child == kinder)
                    return this;
                else
                {
                    var res = child.FindParent(kinder);
                    if (res != null)
                        return res;
                }
            }
            return null;
        }

        /// <summary>
        /// Searches for child's number
        /// </summary>
        /// <param name="kinder"></param>
        /// <returns></returns>
        internal int FindChildrenNumber(Entity kinder)
        {
            for (int i = 0; i < Children.Count; i++)
            {
                if ((object)Children[i] == (object)kinder)
                    return i;
                else
                {
                    var tmp = Children[i].FindChildrenNumber(kinder);
                    if (tmp != -1)
                        return tmp;
                }
            }
            return -1;
        }

        /// <summary>
        /// Unfolds the function into list of nodes. De facto not used yet.
        /// </summary>
        /// <returns></returns>
        public List<Entity> Unfold()
        {
            var res = new List<Entity>();
            var queue = new List<Entity>() { this };
            res.Add(this);
            while (queue.Count > 0)
            {
                var tmp = new List<Entity>();
                foreach (var q in queue)
                    tmp.AddRange(q.Children);
                res.AddRange(tmp);
                queue = tmp;
            }
            return res;
        }

        /// <summary>
        /// Not only checks but also finds subtrees for each key. It is necessary
        /// to keep equal subtrees with equal numbers.
        /// </summary>
        /// <param name="pattern"></param>
        /// <param name="matchings"></param>
        /// <returns></returns>
        internal bool PatternMakeMatch(Pattern pattern, Dictionary<int, Entity> matchings)
        {
            if (pattern.PatternNumber == -1)
            {
                if (pattern.Children.Count != Children.Count)
                    return false;
                for (int i = 0; i < Children.Count; i++)
                {
                    if (pattern.Children[i].entType != EntType.PATTERN)
                        throw new SysException("Numbers in pattern should look like Num(3)");
                    if (!Children[i].PatternMakeMatch((pattern.Children[i] as Pattern), matchings))
                        return false;
                }
            }
            else
            {
                if (!matchings.ContainsKey(pattern.PatternNumber) || matchings[pattern.PatternNumber] == this)
                    matchings[pattern.PatternNumber] = this;
                else
                    return false;
            }
            return true;
        }

        /// <summary>
        /// We have pattern and we have keys. That is the function
        /// to get an expression from the pattern and keys.
        /// </summary>
        /// <param name="keys"></param>
        /// <returns></returns>
        internal Entity BuildTree(Dictionary<int, Entity> keys)
        {
            if (this.entType != Entity.EntType.PATTERN)
                return this;
            if (keys.ContainsKey(PatternNumber))
                return keys[PatternNumber];
            if (PatternType == PatType.NUMBER)
                return new NumberEntity(Number.Parse(Name));
            if (PatternType == PatType.VARIABLE)
                return new VariableEntity(Name);
            var newChildren = new List<Entity>();
            foreach (var child in Children)
            {
                newChildren.Add(child.BuildTree(keys));
            }
            if (PatternType == PatType.FUNCTION)
            {
                var res = new FunctionEntity(Name)
                {
                    Children = newChildren
                };
                return res;
            }
            else if (PatternType == PatType.OPERATOR)
                switch (Name)
                {
                    case "sumf": return newChildren[0] + newChildren[1];
                    case "minusf": return newChildren[0] - newChildren[1];
                    case "mulf": return newChildren[0] * newChildren[1];
                    case "divf": return newChildren[0] / newChildren[1];

                    case "powf": return MathS.Pow(newChildren[0], newChildren[1]);
                    case "logf": return MathS.Log(newChildren[0], newChildren[1]);
                    case "sinf": return MathS.Sin(newChildren[0]);
                    case "cosf": return MathS.Cos(newChildren[0]);
                    case "tanf": return MathS.Tan(newChildren[0]);
                    case "cotanf": return MathS.Cotan(newChildren[0]);
                    case "arcsinf": return MathS.Arcsin(newChildren[0]);
                    case "arccosf": return MathS.Arccos(newChildren[0]);
                    case "arctanf": return MathS.Arctan(newChildren[0]);
                    case "arccotanf": return MathS.Arccotan(newChildren[0]);

                    default: return null;
                }
            else
                return null;
        }
    }

    internal class Pattern : Entity
    {
        public Pattern(int num, PatType type, string name = "") : base(name, EntType.PATTERN) {
            PatternNumber = num;
            PatternType = type;
        }

        internal Dictionary<int, Entity> EqFits(Entity tree)
        {
            // TODO: optimization
            var res = new Dictionary<int, Entity>();
            if (!tree.PatternMakeMatch(this, res))
                return null;
            else
                return res;
        }
        protected override Entity __copy()
        {
            // Actually, no need to implement
            throw new SysException("@");
        }

        protected override bool EqualsTo(Entity obj)
        {
            // Actually, no need to implement
            throw new SysException("@");
        }
        internal override Entity InnerSimplify()
        {
            // Actually, no need to implement
            throw new SysException("@");
        }
        internal override void Check()
        {
            // Actually, no need to implement
            throw new SysException("@");
        }

        public static Pattern operator +(Pattern a, Pattern b) => Sumf.PHang(a, b);
        public static Pattern operator +(Pattern a, Entity b) => Sumf.PHang(a, b);

        public static Pattern operator +(Entity a, Pattern b) => Sumf.PHang(a, b);
        public static Pattern operator -(Pattern a, Pattern b) => Minusf.PHang(a, b);
        public static Pattern operator -(Pattern a, Entity b) => Minusf.PHang(a, b);
        public static Pattern operator -(Entity a, Pattern b) => Minusf.PHang(a, b);
        public static Pattern operator *(Pattern a, Pattern b) => Mulf.PHang(a, b);
        public static Pattern operator *(Pattern a, Entity b) => Mulf.PHang(a, b);
        public static Pattern operator *(Entity a, Pattern b) => Mulf.PHang(a, b);
        public static Pattern operator /(Pattern a, Pattern b) => Divf.PHang(a, b);
        public static Pattern operator /(Pattern a, Entity b) => Divf.PHang(a, b);
        public static Pattern operator /(Entity a, Pattern b) => Divf.PHang(a, b);
    }
}

namespace AngouriMath.Core.TreeAnalysis
{
    internal static partial class TreeAnalyzer
    {
        internal static void ReplaceOneInPlace(ref Entity source, Pattern oldPattern, Entity newPattern)
        {
            var sub = source.FindPatternSubtree(oldPattern);
            if (sub == null) return;

            Dictionary<int, Entity> nodeList;
            try
            {
                nodeList = oldPattern.EqFits(sub);
            }
            catch (SysException error)
            {
                throw new SysException("Error `" + error.Message + "` in pattern " + oldPattern.ToString());
            }
            var newNode = newPattern.BuildTree(nodeList);

            if (oldPattern.Match(source))
            {
                source = newNode;
            }
            else
            {
                var parent = source.FindParent(sub);
                var number = source.FindChildrenNumber(sub);
                parent.Children[number] = newNode;
            }
        }

        /// <summary>
        /// Processes an expression with appropriate rules
        /// </summary>
        /// <param name="rules">
        /// List of Pattern
        /// </param>
        /// <param name="source">
        /// Where to replace in/to
        /// </param>
        /// <returns></returns>
        internal static Entity Replace(RuleList rules, Entity source)
        {
            HashSet<string> replaced = new HashSet<string>();
            var res = source.DeepCopy();
            string hash;
            while (!replaced.Contains(hash = res.GetHashCode()))
            {
                replaced.Add(hash);
                foreach (var pair in rules)
                {
                    ReplaceOneInPlace(ref res, pair.Key, pair.Value);
                }
            }
            return res;
        }

        internal static void ReplaceInPlace(RuleList rules, ref Entity source)
        {
            HashSet<string> replaced = new HashSet<string>();
            string hash;
            while (!replaced.Contains(hash = source.GetHashCode()))
            {
                replaced.Add(hash);
                foreach (var pair in rules)
                {
                    ReplaceOneInPlace(ref source, pair.Key, pair.Value);
                }
            }
        }
    }
}