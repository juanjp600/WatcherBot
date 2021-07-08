namespace Bot600.Monads
{
    public sealed class Ok<T, TErr> : Result<T, TErr>
    {
        public readonly T Value;

        private Ok(T value) => Value = value;

        public static Result<T, TErr> Create(T value) => new Ok<T, TErr>(value);
    }
}
