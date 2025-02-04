using Neo.Cryptography;
using Neo.IO;
using Neo.IO.Json;
using Neo.Ledger;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract.Enumerators;
using Neo.SmartContract.Iterators;
using Neo.SmartContract.Manifest;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Linq;
using VMArray = Neo.VM.Types.Array;

namespace Neo.SmartContract
{
    static partial class InteropService
    {
        public static readonly uint Neo_Native_Deploy = Register("Neo.Native.Deploy", Native_Deploy, 0, TriggerType.Application);
        public static readonly uint Neo_Crypto_ECDsaVerify = Register("Neo.Crypto.ECDsaVerify", Crypto_ECDsaVerify, 0_01000000, TriggerType.All);
        public static readonly uint Neo_Crypto_ECDsaCheckMultiSig = Register("Neo.Crypto.ECDsaCheckMultiSig", Crypto_ECDsaCheckMultiSig, GetECDsaCheckMultiSigPrice, TriggerType.All);
        public static readonly uint Neo_Account_IsStandard = Register("Neo.Account.IsStandard", Account_IsStandard, 0_00030000, TriggerType.All);
        public static readonly uint Neo_Contract_Create = Register("Neo.Contract.Create", Contract_Create, GetDeploymentPrice, TriggerType.Application);
        public static readonly uint Neo_Contract_Update = Register("Neo.Contract.Update", Contract_Update, GetDeploymentPrice, TriggerType.Application);
        public static readonly uint Neo_Storage_Find = Register("Neo.Storage.Find", Storage_Find, 0_01000000, TriggerType.Application);
        public static readonly uint Neo_Enumerator_Create = Register("Neo.Enumerator.Create", Enumerator_Create, 0_00000400, TriggerType.All);
        public static readonly uint Neo_Enumerator_Next = Register("Neo.Enumerator.Next", Enumerator_Next, 0_01000000, TriggerType.All);
        public static readonly uint Neo_Enumerator_Value = Register("Neo.Enumerator.Value", Enumerator_Value, 0_00000400, TriggerType.All);
        public static readonly uint Neo_Enumerator_Concat = Register("Neo.Enumerator.Concat", Enumerator_Concat, 0_00000400, TriggerType.All);
        public static readonly uint Neo_Iterator_Create = Register("Neo.Iterator.Create", Iterator_Create, 0_00000400, TriggerType.All);
        public static readonly uint Neo_Iterator_Key = Register("Neo.Iterator.Key", Iterator_Key, 0_00000400, TriggerType.All);
        public static readonly uint Neo_Iterator_Keys = Register("Neo.Iterator.Keys", Iterator_Keys, 0_00000400, TriggerType.All);
        public static readonly uint Neo_Iterator_Values = Register("Neo.Iterator.Values", Iterator_Values, 0_00000400, TriggerType.All);
        public static readonly uint Neo_Iterator_Concat = Register("Neo.Iterator.Concat", Iterator_Concat, 0_00000400, TriggerType.All);
        public static readonly uint Neo_Json_Serialize = Register("Neo.Json.Serialize", Json_Serialize, 0_00100000, TriggerType.All);
        public static readonly uint Neo_Json_Deserialize = Register("Neo.Json.Deserialize", Json_Deserialize, 0_00500000, TriggerType.All);

        static InteropService()
        {
            foreach (NativeContract contract in NativeContract.Contracts)
                Register(contract.ServiceName, contract.Invoke, contract.GetPrice, TriggerType.System | TriggerType.Application);
        }

        private static long GetECDsaCheckMultiSigPrice(EvaluationStack stack)
        {
            if (stack.Count < 2) return 0;
            var item = stack.Peek(1);
            int n;
            if (item is VMArray array) n = array.Count;
            else n = (int)item.GetBigInteger();
            if (n < 1) return 0;
            return GetPrice(Neo_Crypto_ECDsaVerify, stack) * n;
        }

        private static long GetDeploymentPrice(EvaluationStack stack)
        {
            int size = stack.Peek(0).GetByteLength() + stack.Peek(1).GetByteLength();
            return GasPerByte * size;
        }

        private static bool Native_Deploy(ApplicationEngine engine)
        {
            if (engine.Snapshot.PersistingBlock.Index != 0) return false;
            foreach (NativeContract contract in NativeContract.Contracts)
            {
                engine.Snapshot.Contracts.Add(contract.Hash, new ContractState
                {
                    Script = contract.Script,
                    Manifest = contract.Manifest
                });
                contract.Initialize(engine);
            }
            return true;
        }

        private static bool Crypto_ECDsaVerify(ApplicationEngine engine)
        {
            StackItem item0 = engine.CurrentContext.EvaluationStack.Pop();
            ReadOnlySpan<byte> message = item0 switch
            {
                InteropInterface _interface => _interface.GetInterface<IVerifiable>().GetHashData(),
                Null _ => engine.ScriptContainer.GetHashData(),
                _ => item0.GetSpan()
            };
            ReadOnlySpan<byte> pubkey = engine.CurrentContext.EvaluationStack.Pop().GetSpan();
            ReadOnlySpan<byte> signature = engine.CurrentContext.EvaluationStack.Pop().GetSpan();
            try
            {
                engine.CurrentContext.EvaluationStack.Push(Crypto.VerifySignature(message, signature, pubkey));
            }
            catch (ArgumentException)
            {
                engine.CurrentContext.EvaluationStack.Push(false);
            }
            return true;
        }

        private static bool Crypto_ECDsaCheckMultiSig(ApplicationEngine engine)
        {
            StackItem item0 = engine.CurrentContext.EvaluationStack.Pop();
            ReadOnlySpan<byte> message = item0 switch
            {
                InteropInterface _interface => _interface.GetInterface<IVerifiable>().GetHashData(),
                Null _ => engine.ScriptContainer.GetHashData(),
                _ => item0.GetSpan()
            };
            int n;
            byte[][] pubkeys;
            StackItem item = engine.CurrentContext.EvaluationStack.Pop();
            if (item is VMArray array1)
            {
                pubkeys = array1.Select(p => p.GetSpan().ToArray()).ToArray();
                n = pubkeys.Length;
                if (n == 0) return false;
            }
            else
            {
                n = (int)item.GetBigInteger();
                if (n < 1 || n > engine.CurrentContext.EvaluationStack.Count) return false;
                pubkeys = new byte[n][];
                for (int i = 0; i < n; i++)
                    pubkeys[i] = engine.CurrentContext.EvaluationStack.Pop().GetSpan().ToArray();
            }
            int m;
            byte[][] signatures;
            item = engine.CurrentContext.EvaluationStack.Pop();
            if (item is VMArray array2)
            {
                signatures = array2.Select(p => p.GetSpan().ToArray()).ToArray();
                m = signatures.Length;
                if (m == 0 || m > n) return false;
            }
            else
            {
                m = (int)item.GetBigInteger();
                if (m < 1 || m > n || m > engine.CurrentContext.EvaluationStack.Count) return false;
                signatures = new byte[m][];
                for (int i = 0; i < m; i++)
                    signatures[i] = engine.CurrentContext.EvaluationStack.Pop().GetSpan().ToArray();
            }
            bool fSuccess = true;
            try
            {
                for (int i = 0, j = 0; fSuccess && i < m && j < n;)
                {
                    if (Crypto.VerifySignature(message, signatures[i], pubkeys[j]))
                        i++;
                    j++;
                    if (m - i > n - j)
                        fSuccess = false;
                }
            }
            catch (ArgumentException)
            {
                fSuccess = false;
            }
            engine.CurrentContext.EvaluationStack.Push(fSuccess);
            return true;
        }

        private static bool Account_IsStandard(ApplicationEngine engine)
        {
            UInt160 hash = new UInt160(engine.CurrentContext.EvaluationStack.Pop().GetSpan());
            ContractState contract = engine.Snapshot.Contracts.TryGet(hash);
            bool isStandard = contract is null || contract.Script.IsStandardContract();
            engine.CurrentContext.EvaluationStack.Push(isStandard);
            return true;
        }

        private static bool Contract_Create(ApplicationEngine engine)
        {
            byte[] script = engine.CurrentContext.EvaluationStack.Pop().GetSpan().ToArray();
            if (script.Length > 1024 * 1024) return false;

            var manifest = engine.CurrentContext.EvaluationStack.Pop().GetString();
            if (manifest.Length > ContractManifest.MaxLength) return false;

            UInt160 hash = script.ToScriptHash();
            ContractState contract = engine.Snapshot.Contracts.TryGet(hash);
            if (contract != null) return false;
            contract = new ContractState
            {
                Script = script,
                Manifest = ContractManifest.Parse(manifest)
            };

            if (!contract.Manifest.IsValid(hash)) return false;

            engine.Snapshot.Contracts.Add(hash, contract);
            engine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(contract));
            return true;
        }

        private static bool Contract_Update(ApplicationEngine engine)
        {
            byte[] script = engine.CurrentContext.EvaluationStack.Pop().GetSpan().ToArray();
            if (script.Length > 1024 * 1024) return false;
            var manifest = engine.CurrentContext.EvaluationStack.Pop().GetString();
            if (manifest.Length > ContractManifest.MaxLength) return false;

            var contract = engine.Snapshot.Contracts.TryGet(engine.CurrentScriptHash);
            if (contract is null) return false;

            if (script.Length > 0)
            {
                UInt160 hash_new = script.ToScriptHash();
                if (hash_new.Equals(engine.CurrentScriptHash)) return false;
                if (engine.Snapshot.Contracts.TryGet(hash_new) != null) return false;
                contract = new ContractState
                {
                    Script = script,
                    Manifest = contract.Manifest
                };
                contract.Manifest.Abi.Hash = hash_new;
                engine.Snapshot.Contracts.Add(hash_new, contract);
                if (contract.HasStorage)
                {
                    foreach (var (key, value) in engine.Snapshot.Storages.Find(engine.CurrentScriptHash.ToArray()).ToArray())
                    {
                        engine.Snapshot.Storages.Add(new StorageKey
                        {
                            ScriptHash = hash_new,
                            Key = key.Key
                        }, new StorageItem
                        {
                            Value = value.Value,
                            IsConstant = false
                        });
                    }
                }
                Contract_Destroy(engine);
            }
            if (manifest.Length > 0)
            {
                contract = engine.Snapshot.Contracts.GetAndChange(contract.ScriptHash);
                contract.Manifest = ContractManifest.Parse(manifest);
                if (!contract.Manifest.IsValid(contract.ScriptHash)) return false;
                if (!contract.HasStorage && engine.Snapshot.Storages.Find(engine.CurrentScriptHash.ToArray()).Any()) return false;
            }

            return true;
        }

        private static bool Storage_Find(ApplicationEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                StorageContext context = _interface.GetInterface<StorageContext>();
                if (!CheckStorageContext(engine, context)) return false;
                byte[] prefix = engine.CurrentContext.EvaluationStack.Pop().GetSpan().ToArray();
                byte[] prefix_key = StorageKey.CreateSearchPrefix(context.ScriptHash, prefix);
                StorageIterator iterator = engine.AddDisposable(new StorageIterator(engine.Snapshot.Storages.Find(prefix_key).Where(p => p.Key.Key.AsSpan().StartsWith(prefix)).GetEnumerator()));
                engine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(iterator));
                return true;
            }
            return false;
        }

        private static bool Enumerator_Create(ApplicationEngine engine)
        {
            IEnumerator enumerator;
            switch (engine.CurrentContext.EvaluationStack.Pop())
            {
                case VMArray array:
                    enumerator = new ArrayWrapper(array);
                    break;
                case PrimitiveType primitive:
                    enumerator = new ByteArrayWrapper(primitive);
                    break;
                default:
                    return false;
            }
            engine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(enumerator));
            return true;
        }

        private static bool Enumerator_Next(ApplicationEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                IEnumerator enumerator = _interface.GetInterface<IEnumerator>();
                engine.CurrentContext.EvaluationStack.Push(enumerator.Next());
                return true;
            }
            return false;
        }

        private static bool Enumerator_Value(ApplicationEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                IEnumerator enumerator = _interface.GetInterface<IEnumerator>();
                engine.CurrentContext.EvaluationStack.Push(enumerator.Value());
                return true;
            }
            return false;
        }

        private static bool Enumerator_Concat(ApplicationEngine engine)
        {
            if (!(engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface1)) return false;
            if (!(engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface2)) return false;
            IEnumerator first = _interface1.GetInterface<IEnumerator>();
            IEnumerator second = _interface2.GetInterface<IEnumerator>();
            IEnumerator result = new ConcatenatedEnumerator(first, second);
            engine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(result));
            return true;
        }

        private static bool Iterator_Create(ApplicationEngine engine)
        {
            IIterator iterator;
            switch (engine.CurrentContext.EvaluationStack.Pop())
            {
                case VMArray array:
                    iterator = new ArrayWrapper(array);
                    break;
                case Map map:
                    iterator = new MapWrapper(map);
                    break;
                case PrimitiveType primitive:
                    iterator = new ByteArrayWrapper(primitive);
                    break;
                default:
                    return false;
            }
            engine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(iterator));
            return true;
        }

        private static bool Iterator_Key(ApplicationEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                IIterator iterator = _interface.GetInterface<IIterator>();
                engine.CurrentContext.EvaluationStack.Push(iterator.Key());
                return true;
            }
            return false;
        }

        private static bool Iterator_Keys(ApplicationEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                IIterator iterator = _interface.GetInterface<IIterator>();
                engine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(new IteratorKeysWrapper(iterator)));
                return true;
            }
            return false;
        }

        private static bool Iterator_Values(ApplicationEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                IIterator iterator = _interface.GetInterface<IIterator>();
                engine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(new IteratorValuesWrapper(iterator)));
                return true;
            }
            return false;
        }

        private static bool Iterator_Concat(ApplicationEngine engine)
        {
            if (!(engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface1)) return false;
            if (!(engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface2)) return false;
            IIterator first = _interface1.GetInterface<IIterator>();
            IIterator second = _interface2.GetInterface<IIterator>();
            IIterator result = new ConcatenatedIterator(first, second);
            engine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(result));
            return true;
        }

        private static bool Json_Deserialize(ApplicationEngine engine)
        {
            var json = engine.CurrentContext.EvaluationStack.Pop().GetSpan();
            try
            {
                var obj = JObject.Parse(json, 10);
                var item = JsonSerializer.Deserialize(obj, engine.ReferenceCounter);
                engine.CurrentContext.EvaluationStack.Push(item);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool Json_Serialize(ApplicationEngine engine)
        {
            var item = engine.CurrentContext.EvaluationStack.Pop();
            try
            {
                var json = JsonSerializer.SerializeToByteArray(item, engine.MaxItemSize);
                engine.CurrentContext.EvaluationStack.Push(json);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
