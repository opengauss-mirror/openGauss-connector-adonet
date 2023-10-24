using System;
using System.Collections.Generic;
using System.Data.Common;

namespace OpenGauss.NET
{
    /// <inheritdoc cref="DbBatchCommandCollection"/>
    public class OpenGaussBatchCommandCollection : DbBatchCommandCollection, IList<OpenGaussBatchCommand>
    {
        readonly List<OpenGaussBatchCommand> _list;

        internal OpenGaussBatchCommandCollection(List<OpenGaussBatchCommand> batchCommands)
            => _list = batchCommands;

        /// <inheritdoc/>
        public override int Count => _list.Count;

        /// <inheritdoc/>
        public override bool IsReadOnly => false;

        IEnumerator<OpenGaussBatchCommand> IEnumerable<OpenGaussBatchCommand>.GetEnumerator() => _list.GetEnumerator();

        /// <inheritdoc/>
        public override IEnumerator<DbBatchCommand> GetEnumerator() => _list.GetEnumerator();

        /// <inheritdoc/>
        public void Add(OpenGaussBatchCommand item) => _list.Add(item);

        /// <inheritdoc/>
        public override void Add(DbBatchCommand item) => Add(Cast(item));

        /// <inheritdoc/>
        public override void Clear() => _list.Clear();

        /// <inheritdoc/>
        public bool Contains(OpenGaussBatchCommand item) => _list.Contains(item);

        /// <inheritdoc/>
        public override bool Contains(DbBatchCommand item) => Contains(Cast(item));

        /// <inheritdoc/>
        public void CopyTo(OpenGaussBatchCommand[] array, int arrayIndex) => _list.CopyTo(array, arrayIndex);

        /// <inheritdoc/>
        public override void CopyTo(DbBatchCommand[] array, int arrayIndex)
        {
            if (array is OpenGaussBatchCommand[] typedArray)
            {
                CopyTo(typedArray, arrayIndex);
                return;
            }

            throw new InvalidCastException(
                $"{nameof(array)} is not of type {nameof(OpenGaussBatchCommand)} and cannot be used in this batch command collection.");
        }

        /// <inheritdoc/>
        public int IndexOf(OpenGaussBatchCommand item) => _list.IndexOf(item);

        /// <inheritdoc/>
        public override int IndexOf(DbBatchCommand item) => IndexOf(Cast(item));

        /// <inheritdoc/>
        public void Insert(int index, OpenGaussBatchCommand item) => _list.Insert(index, item);

        /// <inheritdoc/>
        public override void Insert(int index, DbBatchCommand item) => Insert(index, Cast(item));

        /// <inheritdoc/>
        public bool Remove(OpenGaussBatchCommand item) => _list.Remove(item);

        /// <inheritdoc/>
        public override bool Remove(DbBatchCommand item) => Remove(Cast(item));

        /// <inheritdoc/>
        public override void RemoveAt(int index) => _list.RemoveAt(index);

        OpenGaussBatchCommand IList<OpenGaussBatchCommand>.this[int index]
        {
            get => _list[index];
            set => _list[index] = value;
        }

        /// <inheritdoc/>
        public new OpenGaussBatchCommand this[int index]
        {
            get => _list[index];
            set => _list[index] = value;
        }

        /// <inheritdoc/>
        protected override DbBatchCommand GetBatchCommand(int index)
            => _list[index];

        /// <inheritdoc/>
        protected override void SetBatchCommand(int index, DbBatchCommand batchCommand)
            => _list[index] = Cast(batchCommand);

        static OpenGaussBatchCommand Cast(DbBatchCommand? value)
            => value is OpenGaussBatchCommand c
                ? c
                : throw new InvalidCastException(
                    $"The value \"{value}\" is not of type \"{nameof(OpenGaussBatchCommand)}\" and cannot be used in this batch command collection.");
    }
}
