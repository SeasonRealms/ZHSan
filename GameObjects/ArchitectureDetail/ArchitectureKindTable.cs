using System.Collections.Generic;
using System.Runtime.Serialization;

namespace GameObjects.ArchitectureDetail
{
    /// <summary>
    /// 建筑类型表
    /// </summary>
    [DataContract]
    public class ArchitectureKindTable
    {
        [DataMember]
        public Dictionary<int, ArchitectureKind> ArchitectureKinds = new Dictionary<int, ArchitectureKind>();

        /// <summary>
        /// 建筑类型字典添加类型
        /// </summary>
        /// <param name="architectureKind"></param>
        /// <returns></returns>
        public bool AddArchitectureKind(ArchitectureKind architectureKind)
        {
            if (ArchitectureKinds.ContainsKey(architectureKind.ID))
                return false;

            ArchitectureKinds.Add(architectureKind.ID, architectureKind);

            return true;
        }

        /// <summary>
        /// 清空建筑类型字典
        /// </summary>
        public void Clear()
        {
            ArchitectureKinds.Clear();
        }

        /// <summary>
        /// 获取建筑类型
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public ArchitectureKind GetArchitectureKind(int id)
        {
            ArchitectureKinds.TryGetValue(id, out var architectureKind);

            return architectureKind;
        }

        /// <summary>
        /// 获取建筑类型列表
        /// </summary>
        /// <returns></returns>
        public GameObjectList GetArchitectureKindList()
        {
            GameObjectList list = new GameObjectList();

            foreach (ArchitectureKind kind in ArchitectureKinds.Values)
            {
                list.Add(kind);
            }

            return list;
        }
    }
}