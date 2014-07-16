﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Excel = Microsoft.Office.Interop.Excel;
using TreeDict = System.Collections.Generic.Dictionary<AST.Address, DataDebugMethods.TreeNode>;
using RangeDict = System.Collections.Generic.Dictionary<string, DataDebugMethods.TreeNode>;
using System.Diagnostics;

namespace DataDebugMethods
{
    public class AnalysisData
    {
        public List<TreeNode> nodelist;     // holds all the TreeNodes in the Excel file
        public RangeDict input_ranges;
        public TreeDict formula_nodes;
        public TreeDict cell_nodes;
        public Excel.Sheets charts;
        public double tree_construct_time;
        private ProgBar pb;

        private int _pb_max;
        private int _pb_count = 0;

        public AnalysisData(Excel.Application application, Excel.Workbook wb) 
        {
            charts = wb.Charts;
            nodelist = new List<TreeNode>();
            input_ranges = new RangeDict();
            cell_nodes = new TreeDict();
        }

        public AnalysisData(Excel.Application application, Excel.Workbook wb, ProgBar progbar)
            : this (application, wb)
        {
            pb = progbar;
        }

        public void SetProgress(int i)
        {
            if (pb != null) pb.SetProgress(i);
        }

        public void SetPBMax(int max)
        {
            _pb_max = max;
        }

        public void PokePB()
        {
            if (pb != null)
            {
                _pb_count += 1;
                this.SetProgress(_pb_count * 100 / _pb_max);
            }
        }

        private void KillPB()
        {
            // Kill progress bar
            if (pb != null) pb.Close();
        }

        public TreeNode[] TerminalFormulaNodes(bool all_outputs)
        {
            // return only the formula nodes which do not provide
            // input to any other cell and which are also not
            // in our list of excluded functions
            if (all_outputs)
            {
                return formula_nodes.Select(pair => pair.Value).ToArray();
            }
            else
            {
                return formula_nodes.Where(pair => pair.Value.getOutputs().Count == 0)
                                    .Select(pair => pair.Value).ToArray();
            }
        }

        public TreeNode[] TerminalInputNodes()
        {
            // this should filter out the following two cases:
            // 1. input range is intermediate (acts as input to a formula
            //    and also contains a formula which consumes input from
            //    another range).
            // 2. the range is actually a formula cell
            return input_ranges.Where(pair => !pair.Value.GetDontPerturb()
                                              && !pair.Value.isFormula())
                               .Select(pair => pair.Value).ToArray();
        }

        /// <summary>
        /// This method returns all input TreeNodes that are guaranteed to be:
        /// 1. leaf nodes, and
        /// 2. strictly data-containing (no formulas).
        /// </summary>
        /// <returns>TreeNode[]</returns>
        public TreeNode[] TerminalInputCells()
        {
            // this folds all of the inputs for all of the
            // outputs into a set of distinct data-containing cells
            var iecells = TerminalFormulaNodes(true).Aggregate(
                            Enumerable.Empty<TreeNode>(),
                            (acc, node) => acc.Union<TreeNode>(getChildCells(node))
                          );
            return iecells.ToArray<TreeNode>();
        }

        /// <summary>
        ///  This method returns all TreeNodes cells that participate in a computation.  Note
        ///  that these nodes may be formulas!
        /// </summary>
        /// <returnsTreeNode[]></returns>
        public TreeNode[] allComputationCells()
        {
            // this folds all of the inputs for all of the
            // outputs into a set of distinct data-containing cells
            var iecells = TerminalFormulaNodes(true).Aggregate(
                            Enumerable.Empty<TreeNode>(),
                            (acc, node) => acc.Union<TreeNode>(getAllCells(node))
                          );
            return iecells.ToArray<TreeNode>();
        }

        private IEnumerable<TreeNode> getAllCells(TreeNode node)
        {
            var thiscell = node;
            var children = node.getInputs().SelectMany(n => getAllCells(n));
            List<TreeNode> results = new List<TreeNode>(children);
            results.Add(thiscell);
            return results;
        }

        private IEnumerable<TreeNode> getChildCells(TreeNode node)
        {
            // base case: node is a cell (not a range), it has no children, and it's not a formula
            if (node.isCell() && node.getInputs().Count() == 0 && !node.isFormula()) {
                return new List<TreeNode>{node};
            } else {
            // recursive case: node *may* have children; if so, recurse
                var children = node.getInputs().SelectMany(n => getChildCells(n));
                return children;
            }
        }

        public string ToDOT()
        {
            var visited = new HashSet<AST.Address>();
            String s = "digraph spreadsheet {\n";
            foreach (KeyValuePair<AST.Address,TreeNode> pair in formula_nodes)
            {
                s += pair.Value.ToDOT(visited);
            }
            return s + "\n}";
        }

        public bool ContainsLoop()
        {
            var OK = true;
            foreach (KeyValuePair<AST.Address, TreeNode> pair in formula_nodes)
            {
                // a loop is when we see the same node twice while recursing
                var visited_from = new Dictionary<TreeNode,TreeNode>();
                OK = OK && !pair.Value.ContainsLoop(visited_from, null);
            }
            return !OK;
        }
    }
}
