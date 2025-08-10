using System.Collections.Generic;
using Graphs;
using UnityEngine;
using Random = System.Random;

namespace Scripts2D
{
    public class Generator2D : MonoBehaviour {
        enum CellType {
            None,
            Room,
            Hallway
        }

        class Room {
            public RectInt Bounds;

            public Room(Vector2Int location, Vector2Int size) {
                Bounds = new RectInt(location, size);
            }

            public static bool Intersect(Room a, Room b) {
                return !((a.Bounds.position.x >= (b.Bounds.position.x + b.Bounds.size.x)) || ((a.Bounds.position.x + a.Bounds.size.x) <= b.Bounds.position.x)
                    || (a.Bounds.position.y >= (b.Bounds.position.y + b.Bounds.size.y)) || ((a.Bounds.position.y + a.Bounds.size.y) <= b.Bounds.position.y));
            }
        }

        [SerializeField]
        Vector2Int size;
        [SerializeField]
        int roomCount;
        [SerializeField]
        Vector2Int roomMaxSize;
        [SerializeField]
        GameObject cubePrefab;
        [SerializeField]
        Material redMaterial;
        [SerializeField]
        Material blueMaterial;

        private Random _random;
        private Grid2D<CellType> _grid;
        private List<Room> _rooms;
        private Delaunay2D _delaunay;
        private HashSet<Prim.Edge> _selectedEdges;

        private void Start() {
            Generate();
        }

        void Generate() {
            _random = new Random(0);
            _grid = new Grid2D<CellType>(size, Vector2Int.zero);
            _rooms = new List<Room>();

            PlaceRooms();
            Triangulate();
            CreateHallways();
            PathfindHallways();
        }

        private void PlaceRooms() {
            for (int i = 0; i < roomCount; i++) {
                Vector2Int location = new Vector2Int(
                    _random.Next(0, size.x),
                    _random.Next(0, size.y)
                );

                Vector2Int roomSize = new Vector2Int(
                    _random.Next(1, roomMaxSize.x + 1),
                    _random.Next(1, roomMaxSize.y + 1)
                );

                bool add = true;
                Room newRoom = new Room(location, roomSize);
                Room buffer = new Room(location + new Vector2Int(-1, -1), roomSize + new Vector2Int(2, 2));

                foreach (var room in _rooms) {
                    if (Room.Intersect(room, buffer)) {
                        add = false;
                        break;
                    }
                }

                if (newRoom.Bounds.xMin < 0 || newRoom.Bounds.xMax >= size.x
                                            || newRoom.Bounds.yMin < 0 || newRoom.Bounds.yMax >= size.y) {
                    add = false;
                }

                if (add) {
                    _rooms.Add(newRoom);
                    PlaceRoom(newRoom.Bounds.position, newRoom.Bounds.size);

                    foreach (var pos in newRoom.Bounds.allPositionsWithin) {
                        _grid[pos] = CellType.Room;
                    }
                }
            }
        }

        void Triangulate() {
            List<Vertex> vertices = new List<Vertex>();

            foreach (var room in _rooms) {
                vertices.Add(new Vertex<Room>(room.Bounds.position + ((Vector2)room.Bounds.size) / 2, room));
            }

            _delaunay = Delaunay2D.Triangulate(vertices);
        }

        void CreateHallways() {
            List<Prim.Edge> edges = new List<Prim.Edge>();

            foreach (var edge in _delaunay.Edges) {
                edges.Add(new Prim.Edge(edge.U, edge.V));
            }

            List<Prim.Edge> mst = Prim.MinimumSpanningTree(edges, edges[0].U);

            _selectedEdges = new HashSet<Prim.Edge>(mst);
            var remainingEdges = new HashSet<Prim.Edge>(edges);
            remainingEdges.ExceptWith(_selectedEdges);

            foreach (var edge in remainingEdges) {
                if (_random.NextDouble() < 0.125) {
                    _selectedEdges.Add(edge);
                }
            }
        }

        void PathfindHallways() {
            DungeonPathfinder2D aStar = new DungeonPathfinder2D(size);

            foreach (var edge in _selectedEdges) {
                var startRoom = ((Vertex<Room>)edge.U).Item;
                var endRoom = ((Vertex<Room>)edge.V).Item;

                var startPosf = startRoom.Bounds.center;
                var endPosf = endRoom.Bounds.center;
                var startPos = new Vector2Int((int)startPosf.x, (int)startPosf.y);
                var endPos = new Vector2Int((int)endPosf.x, (int)endPosf.y);

                var path = aStar.FindPath(startPos, endPos, (a, b) => {
                    var pathCost = new DungeonPathfinder2D.PathCost
                    {
                        Cost = Vector2Int.Distance(b.Position, endPos) //heuristic
                    };

                    if (_grid[b.Position] == CellType.Room) {
                        pathCost.Cost += 10;
                    } else if (_grid[b.Position] == CellType.None) {
                        pathCost.Cost += 5;
                    } else if (_grid[b.Position] == CellType.Hallway) {
                        pathCost.Cost += 1;
                    }

                    pathCost.Traversable = true;

                    return pathCost;
                });

                if (path != null) {
                    for (int i = 0; i < path.Count; i++) {
                        var current = path[i];

                        if (_grid[current] == CellType.None) {
                            _grid[current] = CellType.Hallway;
                        }

                        if (i > 0) {
                            var prev = path[i - 1];

                            var unused = current - prev;
                        }
                    }

                    foreach (var pos in path) {
                        if (_grid[pos] == CellType.Hallway) {
                            PlaceHallway(pos);
                        }
                    }
                }
            }
        }

        void PlaceCube(Vector2Int location, Vector2Int size, Material material) {
            GameObject go = Instantiate(cubePrefab, new Vector3(location.x, 0, location.y), Quaternion.identity);
            go.GetComponent<Transform>().localScale = new Vector3(size.x, 1, size.y);
            go.GetComponent<MeshRenderer>().material = material;
        }

        void PlaceRoom(Vector2Int location, Vector2Int size) {
            PlaceCube(location, size, redMaterial);
        }

        void PlaceHallway(Vector2Int location) {
            PlaceCube(location, new Vector2Int(1, 1), blueMaterial);
        }
    }
}
