﻿#if UNITY_EDITOR
using ListView;
using System;
using System.Collections.Generic;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Workspaces
{
	sealed class HierarchyListViewController : NestedListViewController<HierarchyData>
	{
		const float k_ClipMargin = 0.001f; // Give the cubes a margin so that their sides don't get clipped

		[SerializeField]
		Material m_TextMaterial;

		[SerializeField]
		Material m_ExpandArrowMaterial;

		int m_SelectedRow;

		readonly Dictionary<int, bool> m_ExpandStates = new Dictionary<int, bool>();

		public Action<int> selectRow;

		protected override void Setup()
		{
			base.Setup();

			m_TextMaterial = Instantiate(m_TextMaterial);
			m_ExpandArrowMaterial = Instantiate(m_ExpandArrowMaterial);
		}

		protected override void UpdateItems()
		{
			var parentMatrix = transform.worldToLocalMatrix;
			SetMaterialClip(m_TextMaterial, parentMatrix);
			SetMaterialClip(m_ExpandArrowMaterial, parentMatrix);

			base.UpdateItems();
		}

		void UpdateHierarchyItem(HierarchyData data, int offset, int depth, bool expanded)
		{
			ListViewItem<HierarchyData> item;
			if (!m_ListItems.TryGetValue(data, out item))
				item = GetItem(data);

			var hierarchyItem = (HierarchyListItem)item;

			hierarchyItem.UpdateSelf(bounds.size.x - k_ClipMargin, depth, expanded, data.instanceID == m_SelectedRow);

			SetMaterialClip(hierarchyItem.cubeMaterial, transform.worldToLocalMatrix);

			UpdateItemTransform(item.transform, offset);
		}

		protected override void UpdateRecursively(List<HierarchyData> data, ref int count, int depth = 0)
		{
			foreach (var datum in data)
			{
				bool expanded;
				if (!m_ExpandStates.TryGetValue(datum.instanceID, out expanded))
					m_ExpandStates[datum.instanceID] = false;

				if (count + m_DataOffset < -1 || count + m_DataOffset > m_NumRows - 1)
					Recycle(datum);
				else
					UpdateHierarchyItem(datum, count, depth, expanded);

				count++;

				if (datum.children != null)
				{
					if (expanded)
						UpdateRecursively(datum.children, ref count, depth + 1);
					else
						RecycleChildren(datum);
				}
			}
		}

		protected override ListViewItem<HierarchyData> GetItem(HierarchyData listData)
		{
			var item = (HierarchyListItem)base.GetItem(listData);
			item.SetMaterials(m_TextMaterial, m_ExpandArrowMaterial);
			item.selectRow = SelectRow;

			item.toggleExpanded = ToggleExpanded;

			bool expanded;
			if (m_ExpandStates.TryGetValue(listData.instanceID, out expanded))
				item.UpdateArrow(expanded, true);

			return item;
		}

		void ToggleExpanded(HierarchyData data)
		{
			var instanceID = data.instanceID;
			m_ExpandStates[instanceID] = !m_ExpandStates[instanceID];
		}

		public void SelectRow(int instanceID)
		{
			if (data == null)
				return;

			m_SelectedRow = instanceID;

			foreach (var datum in data)
			{
				ExpandToRow(datum, instanceID);
			}

			selectRow(instanceID);

			var scrollHeight = 0f;
			foreach (var datum in data)
			{
				ScrollToRow(datum, instanceID, ref scrollHeight);
				scrollHeight += itemSize.z;
			}
		}

		bool ExpandToRow(HierarchyData container, int rowID)
		{
			if (container.instanceID == rowID)
				return true;

			var found = false;
			if (container.children != null)
			{
				foreach (var child in container.children)
				{
					if (ExpandToRow(child, rowID))
						found = true;
				}
			}

			if (found)
				m_ExpandStates[container.instanceID] = true;

			return found;
		}

		void ScrollToRow(HierarchyData container, int rowID, ref float scrollHeight)
		{
			if (container.instanceID == rowID)
			{
				if (-scrollOffset > scrollHeight || -scrollOffset + bounds.size.z < scrollHeight)
					scrollOffset = -scrollHeight;
				return;
			}

			bool expanded;
			m_ExpandStates.TryGetValue(container.instanceID, out expanded);

			if (container.children != null)
			{
				foreach (var child in container.children)
				{
					if (expanded)
					{
						ScrollToRow(child, rowID, ref scrollHeight);
						scrollHeight += itemSize.z;
					}
				}
			}
		}

		private void OnDestroy()
		{
			ObjectUtils.Destroy(m_TextMaterial);
			ObjectUtils.Destroy(m_ExpandArrowMaterial);
		}
	}
}
#endif
