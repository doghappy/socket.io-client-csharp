using FluentAssertions;
using NSubstitute;
using SocketIOClient.Infrastructure;

namespace SocketIOClient.UnitTests.Infrastructure;

public class RandomDelayRetryPolicyTests
{
    public RandomDelayRetryPolicyTests()
    {
        _random = Substitute.For<IRandom>();
        _policy = new RandomDelayRetryPolicy(_random);
    }

    private readonly RandomDelayRetryPolicy _policy;
    private readonly IRandom _random;

    [Fact]
    public async Task RetryAsync_TimesLessThan1_ThrowException()
    {
        var func = Substitute.For<Func<Task>>();

        await _policy
            .Invoking(x => x.RetryAsync(0, func))
            .Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage("Times must be greater than 0 (Parameter 'times')");
    }

    [Fact]
    public async Task RetryAsync_TimesIs2AndNoExceptionWhenFirstCall_FuncIsCalled1Time()
    {
        var func = Substitute.For<Func<Task>>();

        await _policy.RetryAsync(2, func);

        await func.Received(1).Invoke();
        _random.DidNotReceive().Next(Arg.Any<int>());
    }

    [Fact]
    public async Task RetryAsync_FirstThrowThenOk_FuncIsCalled2TimesNoException()
    {
        var func = Substitute.For<Func<Task>>();
        func.Invoke().Returns(Task.FromException<Task>(new InvalidOperationException()), Task.CompletedTask);

        await _policy.RetryAsync(2, func);

        await func.Received(2).Invoke();
        _random.Received(1).Next(Arg.Any<int>());
    }

    [Fact]
    public async Task RetryAsync_FirstOkThenThrow_FuncIsCalled1TimeNoException()
    {
        var func = Substitute.For<Func<Task>>();
        func.Invoke().Returns(Task.CompletedTask, Task.FromException<Task>(new InvalidOperationException()));

        await _policy.RetryAsync(2, func);

        await func.Received(1).Invoke();
        _random.DidNotReceive().Next(Arg.Any<int>());
    }

    [Fact]
    public async Task RetryAsync_Throw2Times_FuncIsCalled2TimesAndThrowException()
    {
        var func = Substitute.For<Func<Task>>();
        func.Invoke().Returns(
            Task.FromException<Task>(new InvalidOperationException("1")),
            Task.FromException<Task>(new InvalidOperationException("2")));

        await _policy
            .Invoking(x => x.RetryAsync(2, func))
            .Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("2");

        await func.Received(2).Invoke();
        _random.Received(1).Next(Arg.Any<int>());
    }
}