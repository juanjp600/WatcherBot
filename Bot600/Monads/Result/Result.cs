using System;

namespace Bot600.Monads
{
    public abstract class Result<T, TErr>
    {
        public static Result<T, TErr> Ok(T value)
        {
            return Ok<T, TErr>.Create(value);
        }

        public static Result<T, TErr> Error(TErr value)
        {
            return Error<T, TErr>.Create(value);
        }

        public override string ToString()
        {
            return this switch
                   {
                       Ok<T, TErr> o    => $"Ok({o.Value?.ToString()})",
                       Error<T, TErr> e => $"Error({e.Value?.ToString()})",
                       _                => throw new ArgumentOutOfRangeException()
                   };
        }

        public Result<TOut, TErr> Map<TOut>(Func<T, TOut> mapping)
        {
            return this switch
                   {
                       Error<T, TErr> e => Result<TOut, TErr>.Error(e.Value),
                       Ok<T, TErr> o    => Result<TOut, TErr>.Ok(mapping(o.Value)),
                       _                => throw new ArgumentOutOfRangeException()
                   };
        }

        public Result<TOut, TErr> Bind<TOut>(Func<T, Result<TOut, TErr>> binder)
        {
            return this switch
                   {
                       Error<T, TErr> e => Result<TOut, TErr>.Error(e.Value),
                       Ok<T, TErr> o    => binder(o.Value),
                       _                => throw new ArgumentOutOfRangeException()
                   };
        }
    }
}
