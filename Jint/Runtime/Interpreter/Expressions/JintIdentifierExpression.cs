using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Esprima.Ast;
using Jint.Native;
using Jint.Native.Argument;
using Jint.Runtime.Environments;

namespace Jint.Runtime.Interpreter.Expressions;

internal sealed class JintIdentifierExpression : JintExpression
{
    private EnvironmentRecord.BindingName _identifier = null!;
    private bool _initialized;

    public JintIdentifierExpression(Identifier expression) : base(expression)
    {
    }

    public EnvironmentRecord.BindingName Identifier
    {
        get
        {
            EnsureIdentifier();
            return _identifier;
        }
    }

    private void Initialize()
    {
        EnsureIdentifier();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureIdentifier()
    {
        _identifier ??= _expression.AssociatedData as EnvironmentRecord.BindingName ?? new EnvironmentRecord.BindingName(((Identifier) _expression).Name);
    }

    public bool HasEvalOrArguments
    {
        get
        {
            var name = ((Identifier) _expression).Name;
            return name is "eval" or "arguments";
        }
    }

    protected override object EvaluateInternal(EvaluationContext context)
    {
        if (!_initialized)
        {
            Initialize();
            _initialized = true;
        }

        var engine = context.Engine;
        var env = engine.ExecutionContext.LexicalEnvironment;
        var strict = StrictModeScope.IsStrictModeCode;
        var identifierEnvironment = JintEnvironment.TryGetIdentifierEnvironmentWithBinding(env, _identifier, out var temp)
            ? temp
            : JsValue.Undefined;

        return engine._referencePool.Rent(identifierEnvironment, _identifier.Value, strict, thisValue: null);
    }

    public override JsValue GetValue(EvaluationContext context)
    {
        // need to notify correct node when taking shortcut
        context.LastSyntaxElement = _expression;

        var identifier = Identifier;
        if (identifier.CalculatedValue is not null)
        {
            return identifier.CalculatedValue;
        }

        var strict = StrictModeScope.IsStrictModeCode;
        var engine = context.Engine;
        var env = engine.ExecutionContext.LexicalEnvironment;

        if (JintEnvironment.TryGetIdentifierEnvironmentWithBindingValue(
                env,
                identifier,
                strict,
                out _,
                out var value))
        {
            if (value is null)
            {
                ThrowNotInitialized(engine);
            }
        }
        else
        {
            var reference = engine._referencePool.Rent(JsValue.Undefined, identifier.Value, strict, thisValue: null);
            value = engine.GetValue(reference, true);
        }

        // make sure arguments access freezes state
        if (value is ArgumentsInstance argumentsInstance)
        {
            argumentsInstance.Materialize();
        }

        return value;
    }

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void ThrowNotInitialized(Engine engine)
    {
        ExceptionHelper.ThrowReferenceError(engine.Realm, $"{_identifier.Key.Name} has not been initialized");
    }
}
