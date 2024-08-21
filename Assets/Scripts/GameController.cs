using m039.Common.Pathfindig;
using Pathfinding;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace Game
{
    public class GameController : MonoBehaviour
    {
        #region Inspector

        [SerializeField]
        GridView _GridView;

        [SerializeField]
        LineRenderer _LineRenderer;

        [SerializeField]
        float _GenerationRatio = 0.2f;

        [SerializeField]
        TMPro.TMP_Dropdown _SearchVariant;

        [SerializeField]
        GraphController _GraphController;

        #endregion

        Node _currentNode;

        Node _goalNode;

        Node _startNode;

        static readonly RaycastHit2D[] s_Buffer = new RaycastHit2D[16];

        void Start()
        {
            Init();
        }

        void Init()
        {
            var data = StorageManager.Load();
            if (data == null)
            {
                var grids = new bool[_GridView.columns, _GridView.rows];
                _GridView.CreateGrid(grids);

                _startNode = _GridView.Nodes[0, 0];
                _startNode.cell.SetState(GridCell.CellState.StartNode);
            } else
            {
                OnLoadClicked();
            }
        }

        void Update()
        {
            ProcessInput();
        }

        Coroutine _findPathCoroutine;

        GridGraph _gridGraph;

        public void OnFindPathClicked()
        {
            if (_goalNode == null)
            {
                Debug.LogWarning("Can't find a path, the goal node is missing!");
                return;
            }

            var variant = _SearchVariant.options[_SearchVariant.value].text;

            if (variant == "Common Pathfinding")
            {
                _GraphController.width = _GridView.width;
                _GraphController.height = _GridView.height;
                _GraphController.columns = _GridView.columns;
                _GraphController.rows = _GridView.rows;

                var width = _GridView.columns;
                var height = _GridView.rows;

                var grids = new int[width, height];
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        grids[x, y] = _GridView.Nodes[x, y].state == NodeState.Blocked ? 1 : 0;
                    }
                }

                var graph = new Graph(_GraphController, grids);
                var pathfinder = graph.CreatePahtfinder();

                float time = Time.realtimeSinceStartup;
                var path = pathfinder.Search(graph.GetNode(_startNode.x, _startNode.y), graph.GetNode(_goalNode.x, _goalNode.y));
                Debug.Log("Common Pathfinding: elapse time = " + ((Time.realtimeSinceStartup - time) * 1000) + " ms.");
                DrawPath(path.vectorPath);
            } else if (variant == "A* Pathfinding Project")
            {
                if (_gridGraph == null)
                {
                    AstarData data = AstarPath.active.data;
                    _gridGraph = data.gridGraph;
                }
                var width = _GridView.columns;
                var height = _GridView.rows;
                var nodeSize = _GridView.height / height;
                //_gridGraph.SetDimensions(width, height, nodeSize);

                AstarPath.active.Scan();

                for (int z = 0; z < height; z++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        var node = _gridGraph.GetNode(x, z);
                        node.Walkable = _GridView.Nodes[x, z].state == NodeState.Open;
                    }
                }

                _gridGraph.GetNodes(node => _gridGraph.CalculateConnections((GridNodeBase)node));

                float time = Time.realtimeSinceStartup;

                var p = ABPath.Construct(_startNode.cell.transform.position, _goalNode.cell.transform.position, p =>
                {
                    Debug.Log("A* Project Pathfinding: elapse time = " + ((Time.realtimeSinceStartup - time) * 1000) + " ms.");

                    DrawPath(p.vectorPath);
                });

                AstarPath.StartPath(p);

            } else if (variant == "Debug Pathfinding")
            {
                _findPathCoroutine = StartCoroutine(FindPathCoroutine());

            }
        }

        IEnumerator FindPathCoroutine()
        {
            _LineRenderer.positionCount = 0;

            var pathfinder = new Pathfinder(this, _GridView);

            yield return pathfinder.Search(_startNode, _goalNode);

            _findPathCoroutine = null;
        }

        public void OnStopFindingPathClicked()
        {
            if (_findPathCoroutine != null)
            {
                StopCoroutine(_findPathCoroutine);
                _findPathCoroutine = null;
            }
        }

        public void OnSaveClicked()
        {
            if (_startNode == null || _goalNode == null)
            {
                Debug.LogError("Can't save level, start or goal node is empty.");
                return;
            }

            var nodes = _GridView.Nodes;
            var width = nodes.GetLength(0);
            var height = nodes.GetLength(1);
            var grids = new bool[width, height];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    grids[x, y] = nodes[x, y].state == NodeState.Blocked;
                }
            }
            StorageManager.Save(grids, _startNode, _goalNode);
        }

        public void OnGenerateClicked()
        {
            var width = _GridView.columns;
            var height = _GridView.rows;
            var grids = new bool[width, height];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (x == 0 && y == 0)
                        continue;

                    grids[x, y] = UnityEngine.Random.Range(0f, 1f) < _GenerationRatio;
                }
            }

            _GridView.CreateGrid(grids);

            _startNode = _GridView.Nodes[0, 0];
            _startNode.cell.SetState(GridCell.CellState.StartNode);

            _goalNode = null;
        }

        public void OnLoadClicked()
        {
            var data = StorageManager.Load();
            if (data == null)
            {
                Debug.LogWarning("The data is empty.");
                return;
            }

            var grids = new bool[data.width, data.height];
            for (int x = 0; x < data.width; x++)
            {
                for (int y = 0; y < data.height; y++)
                {
                    grids[x, y] = data.grids[y * data.width + x] == 1;
                }
            }

            _GridView.width = data.width / (float) data.height * _GridView.height;

            _GridView.CreateGrid(grids);

            _startNode = _GridView.Nodes[data.start.x, data.start.y];
            _startNode.cell.SetState(GridCell.CellState.StartNode);

            _goalNode = _GridView.Nodes[data.goal.x, data.goal.y];
            _goalNode.cell.SetState(GridCell.CellState.GoalNode);
        }

        void ProcessInput()
        {
            // The animation is running.
            if (_findPathCoroutine != null)
                return;

            if (Input.GetMouseButtonDown(0))
            {
                if (_currentNode != null)
                {
                    if (_currentNode != _startNode && _currentNode != _goalNode)
                    {
                        if (_goalNode != null)
                        {
                            _goalNode.cell.SetState(GridCell.CellState.Empty);
                        }
                        _goalNode = _currentNode;
                        _goalNode.cell.SetState(GridCell.CellState.GoalNode);
                    } else if (_currentNode == _goalNode)
                    {
                        _goalNode.cell.SetState(GridCell.CellState.Empty);
                        _goalNode = null;
                    }
                }
            }

            if (Input.GetMouseButtonDown(1))
            {
                if (_currentNode != null)
                {
                    if (_currentNode != _startNode && _currentNode != _goalNode)
                    {
                        if (_currentNode.state == NodeState.Open)
                        {
                            _currentNode.state = NodeState.Blocked;
                            _currentNode.cell.SetState(GridCell.CellState.Blocked);
                        } else
                        {
                            _currentNode.state = NodeState.Open;
                            _currentNode.cell.SetState(GridCell.CellState.Empty);
                        }
                    }
                }
            }

            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            var count = Physics2D.GetRayIntersectionNonAlloc(ray, s_Buffer);
            for (int i = 0; i < count; i++)
            {
                if (s_Buffer[i].collider.GetComponentInParent<GridCell>() is GridCell gridCell)
                {
                    if (_currentNode != null && _currentNode != gridCell.node)
                    {
                        _currentNode.cell.SetHighlighted(false);
                        _currentNode = null;
                    }

                    if (_currentNode == null)
                    {
                        _currentNode = gridCell.node;
                        _currentNode.cell.SetHighlighted(true);
                    }

                    return;
                }
            }

            if (_currentNode != null)
            {
                _currentNode.cell.SetHighlighted(false);
                _currentNode = null;
            }
        }

        public void DrawPath(List<Vector3> path)
        {
            if (path != null)
            {
                _LineRenderer.positionCount = path.Count;
                for (int i = 0; i < path.Count; i++)
                {
                    _LineRenderer.SetPosition(i, path[i]);
                }
            } else
            {
                _LineRenderer.positionCount = 0;
            }
        }
    }
}
