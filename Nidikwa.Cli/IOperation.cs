﻿using System.Reflection;

namespace Nidikwa.Cli;

internal interface IOperation
{
    delegate IOperation OperationContructor();
    private static (OperationAttribute Metadata, OperationContructor Constructor)[]? allOperations;
    public static (OperationAttribute Metadata, OperationContructor Constructor)[] AllOperations
    {
        get
        {
            return allOperations ??= typeof(Program).Assembly.GetTypes()
                .Where(type =>
                {
                    if (!type.GetInterfaces().Contains(typeof(IOperation)))
                        return false;
                    if (type.GetCustomAttribute<OperationAttribute>() is null)
                        return false;
                    return true;
                })
                .Select<Type, (OperationAttribute, OperationContructor)>(type => (type.GetCustomAttribute<OperationAttribute>()!, () => (IOperation)type.GetConstructor([])!.Invoke(null)))
                .ToArray();

        }
    }
    public Task ExecuteAsync(string[] args);
}
