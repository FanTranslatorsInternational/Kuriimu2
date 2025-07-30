using System.Reflection.Emit;
using SixLabors.ImageSharp;

// TODO: Optimization possible for consecutive power of 2 coordinates in the same dimension
namespace Kanvas.Swizzle
{
    /// <summary>
    /// The MasterSwizzle on which every other swizzle should be based on.
    /// </summary>
    public class MasterSwizzle
    {
        private readonly Func<int, Point> _transform;

        /// <summary>
        /// Width of the macro tile.
        /// </summary>
        public int MacroTileWidth { get; }

        /// <summary>
        /// Height of the macro tile.
        /// </summary>
        public int MacroTileHeight { get; }

        /// <summary>
        /// Creates an instance of MasterSwizzle.
        /// </summary>
        /// <param name="imageStride">Pixel count of dimension in which should get aligned.</param>
        /// <param name="init">The initial point, where the swizzle begins.</param>
        /// <param name="bitFieldCoords">Array of coordinates, assigned to every bit in the macroTile.</param>
        /// <param name="initPointTransformOnY">Defines a transformation array of the initial point with changing Y.</param>
        public MasterSwizzle(int imageStride, Point init, (int, int)[] bitFieldCoords, (int, int)[] initPointTransformOnY = null)
        {
            MacroTileWidth = bitFieldCoords.Aggregate(0, (x, y) => x | y.Item1) + 1;
            MacroTileHeight = bitFieldCoords.Aggregate(0, (x, y) => x | y.Item2) + 1;

            var widthInTiles = (imageStride + MacroTileWidth - 1) / MacroTileWidth;
            _transform = EmitTransformationMethod(init, bitFieldCoords, initPointTransformOnY, widthInTiles);
        }

        /// <summary>
        /// Transforms a given pointCount into a point
        /// </summary>
        /// <param name="pointCount">The overall pointCount to be transformed</param>
        /// <returns>The Point, which got calculated by given settings</returns>
        public Point Get(int pointCount) => _transform(pointCount);

        private Func<int, Point> EmitTransformationMethod(Point initPoint, (int, int)[] bitField, (int, int)[] initPointTransformOnY, int widthInTiles)
        {
            // Create public static method to transform the point
            var dynamicMethod = new DynamicMethod("Get", typeof(Point), [typeof(int)]);
            var method = dynamicMethod.GetILGenerator();

            // Prepare some variables
            var pointsInMacroBlock = MacroTileWidth * MacroTileHeight;

            var localMacroTileCount = method.DeclareLocal(typeof(int));
            var localMacroX = method.DeclareLocal(typeof(int));
            var localMacroY = method.DeclareLocal(typeof(int));

            method.Emit(OpCodes.Ldarg_0);
            method.Emit(OpCodes.Ldc_I4, pointsInMacroBlock);
            method.Emit(OpCodes.Div_Un);
            method.Emit(OpCodes.Stloc, localMacroTileCount);

            method.Emit(OpCodes.Ldloc, localMacroTileCount);
            method.Emit(OpCodes.Ldc_I4, widthInTiles);
            method.Emit(OpCodes.Rem_Un);
            method.Emit(OpCodes.Stloc, localMacroX);

            method.Emit(OpCodes.Ldloc, localMacroTileCount);
            method.Emit(OpCodes.Ldc_I4, widthInTiles);
            method.Emit(OpCodes.Div_Un);
            method.Emit(OpCodes.Stloc, localMacroY);

            // Modify x
            method.Emit(OpCodes.Ldc_I4, initPoint.X);

            //   Add x macro block information
            method.Emit(OpCodes.Ldloc, localMacroX);
            method.Emit(OpCodes.Ldc_I4, MacroTileWidth);
            method.Emit(OpCodes.Mul);
            method.Emit(OpCodes.Xor);

            //   Process x bit field
            foreach (var element in bitField.Select((x, i) => (x, i)).Where(x => x.x.Item1 != 0))
            {
                EmitCoordinateTransformation(method, element.i, bitField[element.i].Item1);
                method.Emit(OpCodes.Xor);
            }

            //   Process x init transformation
            if (initPointTransformOnY != null)
            {
                foreach (var element in initPointTransformOnY.Select((x, i) => (x, i)).Where(x => x.x.Item1 != 0))
                {
                    EmitCoordinateTransformation(method, localMacroY, element.i, initPointTransformOnY[element.i].Item1);
                    method.Emit(OpCodes.Xor);
                }
            }

            // Modify y
            method.Emit(OpCodes.Ldc_I4, initPoint.Y);

            //   Add y macro block information
            method.Emit(OpCodes.Ldloc, localMacroY);
            method.Emit(OpCodes.Ldc_I4, MacroTileHeight);
            method.Emit(OpCodes.Mul);
            method.Emit(OpCodes.Xor);

            //   Process y bit field
            foreach (var element in bitField.Select((x, i) => (x, i)).Where(x => x.x.Item2 != 0))
            {
                EmitCoordinateTransformation(method, element.i, bitField[element.i].Item2);
                method.Emit(OpCodes.Xor);
            }

            //   Process y init transformation
            if (initPointTransformOnY != null)
            {
                foreach (var element in initPointTransformOnY.Select((x, i) => (x, i)).Where(x => x.x.Item2 != 0))
                {
                    EmitCoordinateTransformation(method, localMacroY, element.i, initPointTransformOnY[element.i].Item2);
                    method.Emit(OpCodes.Xor);
                }
            }

            // Create result
            method.Emit(OpCodes.Newobj, typeof(Point).GetConstructor([typeof(int), typeof(int)]));

            // Return
            method.Emit(OpCodes.Ret);

            return (Func<int, Point>)dynamicMethod.CreateDelegate(typeof(Func<int, Point>));
        }

        private void EmitCoordinateTransformation(ILGenerator method, int index, int coordinate)
        {
            method.Emit(OpCodes.Ldarg_0);

            method.Emit(OpCodes.Ldc_I4, index);
            method.Emit(OpCodes.Shr_Un);

            method.Emit(OpCodes.Ldc_I4, 2);
            method.Emit(OpCodes.Rem_Un);

            method.Emit(OpCodes.Ldc_I4, coordinate);
            method.Emit(OpCodes.Mul);
        }

        private void EmitCoordinateTransformation(ILGenerator method, LocalBuilder initialValue, int index, int coordinate)
        {
            method.Emit(OpCodes.Ldloc, initialValue);

            method.Emit(OpCodes.Ldc_I4, index);
            method.Emit(OpCodes.Shr_Un);

            method.Emit(OpCodes.Ldc_I4, 2);
            method.Emit(OpCodes.Rem_Un);

            method.Emit(OpCodes.Ldc_I4, coordinate);
            method.Emit(OpCodes.Mul);
        }
    }
}
