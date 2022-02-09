﻿// Copyright © 2012-2022 VLINGO LABS. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;

namespace Vlingo.Xoom.Common
{
    public class Failure<TCause, TValue> : IOutcome<TCause, TValue> where TCause : Exception
    {
        private readonly TCause _cause;

        internal Failure(TCause cause) => _cause = cause;

        public virtual IOutcome<TCause, Tuple<TValue, TSecondSuccess>> AlongWith<TOtherFailure, TSecondSuccess>(IOutcome<TOtherFailure, TSecondSuccess> outcome) where TOtherFailure : Exception => 
            Failure.Of<TCause, Tuple<TValue, TSecondSuccess>>(_cause);

        public virtual IOutcome<TCause, TNextSuccess> AndThen<TNextSuccess>(Func<TValue, TNextSuccess> action) => 
            Failure.Of<TCause, TNextSuccess>(_cause);

        public virtual IOutcome<TNextFailure, TNextSuccess> AndThenTo<TNextFailure, TNextSuccess>(Func<TValue, IOutcome<TNextFailure, TNextSuccess>> action) where TNextFailure : Exception => 
            Failure.Of<TNextFailure, TNextSuccess>((TNextFailure)(Exception)_cause);

        public virtual ICompletes<TValue> AsCompletes() => Completes.WithFailure<TValue>();

        public virtual Optional<TValue> AsOptional() => Optional.Empty<TValue>();

        public virtual void AtLeastConsume(Action<TValue> consumer)
        {
        }

        public virtual IOutcome<NoSuchElementException, TValue> Filter(Func<TValue, bool> filterFunction) => 
            Failure.Of<NoSuchElementException, TValue>(new NoSuchElementException(_cause));

        public virtual TValue Get() => throw _cause;

        public virtual TValue GetOrNull() => default!;

        public virtual IOutcome<TCause, TValue> Otherwise(Func<TCause, TValue> action) => Success.Of<TCause, TValue>(action.Invoke(_cause));

        public virtual IOutcome<TNextFailure, TNextSuccess> OtherwiseTo<TNextFailure, TNextSuccess>(Func<TCause, IOutcome<TNextFailure, TNextSuccess>> action) where TNextFailure : Exception => 
            action.Invoke(_cause);

        public virtual TNextSuccess Resolve<TNextSuccess>(Func<TCause, TNextSuccess> onFailedOutcome, Func<TValue, TNextSuccess> onSuccessfulOutcome) => 
            onFailedOutcome.Invoke(_cause);

        public virtual IOutcome<TNextFailure, TValue> OtherwiseFail<TNextFailure>(Func<TCause, TNextFailure> action) 
            where TNextFailure : Exception
            => Failure.Of<TNextFailure, TValue>(action.Invoke(_cause));

        public override bool Equals(object? obj)
        {
            var other = obj as Failure<TCause, TValue>;

            if (other == null)
            {
                return false;
            }

            return Equals(_cause, other._cause);
        }

        public override int GetHashCode()
        {
            return 31 * _cause.GetHashCode();
        }
    }

    public static class Failure
    {
        public static IOutcome<TCause, TValue> Of<TCause, TValue>(TCause cause) where TCause : Exception => 
            new Failure<TCause, TValue>(cause);
    }
}
