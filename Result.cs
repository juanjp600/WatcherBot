using System;

namespace Bot600
{
    public class Result<T>
    {
        public readonly bool IsSuccess;
        public readonly string FailureMessage;
        public readonly T Value;

        public override string ToString()
        {
            return IsSuccess ? Value.ToString() : FailureMessage;
        }

        protected Result(string failureMessage)
        {
            IsSuccess = false;
            FailureMessage = failureMessage;
        }

        protected Result(T successValue)
        {
            IsSuccess = true;
            Value = successValue;
        }

        public static Result<T> Success(T value)
        {
            return new Result<T>(value);
        }

        public static Result<T> Failure(string message)
        {
            return new Result<T>(message);
        }

        public Result<TOut> Map<TOut>(Func<T, TOut> func)
        {
            return IsSuccess ? Result<TOut>.Success(func(Value)) : Result<TOut>.Failure(FailureMessage);
        }

        public Result<TOut> Bind<TOut>(Func<T, Result<TOut>> func)
        {
            return IsSuccess ? func(Value) : Result<TOut>.Failure(FailureMessage);
        }

        public Result<T> OrElseThunk(Func<Result<T>> other)
        {
            return IsSuccess ? this : other();
        }
    }
}
