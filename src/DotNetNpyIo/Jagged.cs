namespace DotNetNpyIo
{
    public static class Jagged
    {
        public static T[][] Create<T>(int ni, int nj)
        {
            return Create<T[][]>(new[] { ni, nj });
        }

        public static T[][][] Create<T>(int ni, int nj, int nk)
        {
            return Create<T[][][]>(new[] { ni, nj, nk });
        }

        public static T[][][][] Create<T>(int ni, int nj, int nk, int nl)
        {
            return Create<T[][][][]>(new[] { ni, nj, nk, nl });
        }

        public static T[][][][][] Create<T>(int ni, int nj, int nk, int nl, int nm)
        {
            return Create<T[][][][][]>(new[] { ni, nj, nk, nl, nm });
        }

        private static T Create<T>(params int[] arrayDimensions)
        {
            return (T)CreateJaggedArray(typeof(T).GetElementType(), 0, arrayDimensions);
        }

        private static object CreateJaggedArray(Type type, int index, int[] lengths)
        {
            Array array = Array.CreateInstance(type, lengths[index]);
            Type elementType = type.GetElementType();

            if (elementType != null)
            {
                for (int i = 0; i < lengths[index]; i++)
                {
                    array.SetValue(CreateJaggedArray(elementType, index + 1, lengths), i);
                }
            }

            return array;
        }
    }
}