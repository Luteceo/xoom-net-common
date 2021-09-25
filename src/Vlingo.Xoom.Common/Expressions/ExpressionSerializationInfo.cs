// Copyright © 2012-2021 VLINGO LABS. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;

namespace Vlingo.Xoom.Common.Expressions
{
    public class ExpressionSerializationInfo
    {
        public string MethodName { get; }
        public object?[] Args { get; }
        public Type?[] Types { get; }
        public Type?[] ArgumentTypes { get; }

        public ExpressionSerializationInfo(string methodName, object?[] args, Type?[] types, Type?[] argumentTypes)
        {
            MethodName = methodName;
            Args = args;
            Types = types;
            ArgumentTypes = argumentTypes;
        }
    }
}