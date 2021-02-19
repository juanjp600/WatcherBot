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

        public static bool operator ==(Result<T> a, Result<T> b)
        {
            if (a is null && b is null)
                return true;
            if (a is null || b is null)
                return false;
            if (a.IsSuccess != b.IsSuccess)
                return false;
            if (a.IsSuccess)
                return a.Value.Equals(b.Value);
            return a.FailureMessage == b.FailureMessage;
        }
        
        public static bool operator !=(Result<T> a, Result<T> b)
        {
            return !(a == b);
        }

        public override bool Equals(object obj)
        {
            return this == (Result<T>) obj;
        }

        public override int GetHashCode()
        {
            var baseHash = IsSuccess ? Value.GetHashCode() : FailureMessage.GetHashCode();
            var lsb = IsSuccess ? 1 : 0;
            return (baseHash << 1) | lsb;
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

        public Result<T> BindError(Func<string, Result<T>> other)
        {
            return IsSuccess ? this : other(FailureMessage);
        }
    }
}
