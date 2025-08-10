using UnityEngine;

namespace Scripts2D
{
    public class Grid2D<T> {
        private readonly T[] _data;

        public Vector2Int Size { get; private set; }
        private Vector2Int Offset { get; set; }

        public Grid2D(Vector2Int size, Vector2Int offset) {
            Size = size;
            Offset = offset;

            _data = new T[size.x * size.y];
        }

        private int GetIndex(Vector2Int pos) {
            return pos.x + (Size.x * pos.y);
        }

        public bool InBounds(Vector2Int pos) {
            return new RectInt(Vector2Int.zero, Size).Contains(pos + Offset);
        }

        public T this[int x, int y] {
            get => this[new Vector2Int(x, y)];
            set => this[new Vector2Int(x, y)] = value;
        }

        public T this[Vector2Int pos] {
            get {
                pos += Offset;
                return _data[GetIndex(pos)];
            }
            set {
                pos += Offset;
                _data[GetIndex(pos)] = value;
            }
        }
    }
}
