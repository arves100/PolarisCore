﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;

using Xunit;

using Polaris.Lib.Packet.Common;
using Polaris.Lib.Packet.Packets;
using Polaris.Lib.Utility;
using Polaris.Server.Modules.Ship;
using System.Runtime.InteropServices;
using Polaris.Lib.Data;
using Polaris.Lib.Extensions;
using System.Net;

namespace Tests
{
	public class Tests
	{
		#region Polaris.Lib
		#region FreeList

		[Fact]
		public void PolarisLib_Utility_FreeListTestConstructor()
		{
			FreeList<int> list = new FreeList<int>(123);
			Assert.True(list.CurrentSize == 0);
			Assert.True(list.FreeSpace == 123);
			Assert.True(list.MaxSize == 123);
		}

		[Fact]
		public void PolarisLib_Utility_FreeListTestAdd()
		{
			FreeList<int> list = new FreeList<int>(1);
			FreeList<int> list2 = new FreeList<int>(2);

			Assert.True(list.Add(1) == 0);
			Assert.True(list[0] == 1);
			Assert.True(list.CurrentSize == 1);
			Assert.True(list.FreeSpace == 0);
			Assert.True(list.Add(2) == -1);

			Assert.True(list2.Add(1) == 0);
			Assert.True(list2.Add(2) == 1);
			Assert.True(list2.Add(3) == -1);
			Assert.True(list2[0] == 1);
			Assert.True(list2[1] == 2);
		}

		[Fact]
		public void PolarisLib_Utility_FreeListTestRemove()
		{
			FreeList<int> list = new FreeList<int>(1);
			FreeList<int> list2 = new FreeList<int>(2);

			list.Remove(list.Add(1));
			Assert.True(list.FreeSpace == 1);
			Assert.True(list.CurrentSize == 0);

			list2.Add(1);
			list2.Add(2);
			list2.Remove(0);
			Assert.True(list2.FreeSpace == 1);
			Assert.True(list2.CurrentSize == 1);
			Assert.True(list2[1] == 2);
			list2.Add(3);
			Assert.True(list2[0] == 3);
		}

		[Fact]
		public void PolarisLib_Utility_FreeListTestAddClassTypes()
		{
			FreeList<List<int>> list = new FreeList<List<int>>(1);
			FreeList<List<int>> list2 = new FreeList<List<int>>(3);

			List<int> l1 = new List<int>();
			l1.Add(1);
			list.Add(l1);
			Assert.True(list[0][0] == 1);
			Assert.True(list.FreeSpace == 0);
			Assert.True(list.CurrentSize == 1);

			List<int> l2 = new List<int>();
			l2.Add(1);
			l2.Add(2);
			list2.Add(l1);
			list2.Add(l2);
			Assert.True(list2[0][0] == 1);
			Assert.True(list2[1][0] == 1);
			Assert.True(list2[1][1] == 2);
			Assert.True(list2.FreeSpace == 1);
			Assert.True(list2.CurrentSize == 2);

			l2.Add(3);
			Assert.True(list2[1][2] == 3);
		}

		#endregion FreeList

		#region Structure

		[Fact]
		public void PolarisLib_Utility_Structure_TestByteArrayToStructure()
		{
			PacketHeader header = Structure.ByteArrayToStructure<PacketHeader>(new byte[] { 0x50, 0x02, 0x00, 0x00, 0x11, 0x3D, 0x04, 0x00 });

			// [Size:4][Type:1][SubType:1][Flag1:1][Flag2:1]
			Assert.True(header.size == 0x00000250);
			Assert.True(header.type == 0x11);
			Assert.True(header.subType == 0x3D);
			Assert.True(header.flag1 == 0x04);
			Assert.True(header.flag2 == 0x00);
		}

		[Fact]
		public void PolarisLib_Utility_Structure_TestStructureToByteArray()
		{
			PacketHeader header = new PacketHeader();
			header.size = 0x00000250;
			header.type = 0x11;
			header.subType = 0x3D;
			header.flag1 = 0x04;
			header.flag2 = 0x00;

			Assert.True(Structure.StructureToByteArray(header).SequenceEqual(new byte[] { 0x50, 0x02, 0x00, 0x00, 0x11, 0x3D, 0x04, 0x00 }));
		}

		#endregion Structure

		#region Packet and PacketHeader

		[Fact]
		public void PolarisLib_Packet_PacketBaseTestConstructorRecv()
		{
			byte[] clientPacket =
			{
				0x18, 0x00, 0x00, 0x00,
				0x0E, 0x19, 0x40, 0x00,
				0x00, 0x00, 0x00, 0x00,
				0x00, 0x00, 0x00, 0x00,
				0x00, 0x24, 0x00, 0x00,
				0x00, 0x00, 0x00, 0x00
			};
			PacketBase pkt = new PacketBase(clientPacket);

			// [Size:4][Type:1][SubType:1][Flag1:1][Flag2:1]
			Assert.True(pkt.Header.size == 0x18);
			Assert.True(pkt.Header.type == 0x0E);
			Assert.True(pkt.Header.subType == 0x19);
			Assert.True(pkt.Header.flag1 == 0x40);
			Assert.True(pkt.Header.flag2 == 0x00);

			byte[] data =
			{
				0x00, 0x00, 0x00, 0x00,
				0x00, 0x00, 0x00, 0x00,
				0x00, 0x24, 0x00, 0x00,
				0x00, 0x00, 0x00, 0x00
			};
			Assert.True(pkt.Payload.SequenceEqual(data));
			Assert.True(pkt.Packet().SequenceEqual(clientPacket));
		}

		[Fact]
		public void PolarisLib_Packet_PacketBaseTestConstructorSend()
		{
			byte[] shipPacket =
			{
				0x50, 0x02, 0x00, 0x00, 0x11, 0x3D, 0x04, 0x00, 0x44, 0xE4, 0x00, 0x00, 0x28, 0x23, 0x00, 0x00,
				0x53, 0x00, 0x68, 0x00, 0x69, 0x00, 0x70, 0x00, 0x30, 0x00, 0x39, 0x00, 0x00, 0x00, 0x00, 0x00,
				0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
				0xD2, 0xBD, 0xD0, 0x79, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x02, 0x00, 0x95, 0x97, 0x09, 0x00,
				0x70, 0x17, 0x00, 0x00, 0x53, 0x00, 0x68, 0x00, 0x69, 0x00, 0x70, 0x00, 0x30, 0x00, 0x36, 0x00,
				0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
				0x00, 0x00, 0x00, 0x00, 0xD2, 0xBD, 0xD0, 0x4C, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x05, 0x00,
				0x25, 0xD4, 0x08, 0x00, 0x88, 0x13, 0x00, 0x00, 0x53, 0x00, 0x68, 0x00, 0x69, 0x00, 0x70, 0x00,
				0x30, 0x00, 0x35, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
				0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xD2, 0xBD, 0xD0, 0x3D, 0x00, 0x00, 0x00, 0x00,
				0x01, 0x00, 0x06, 0x00, 0xD1, 0x95, 0x09, 0x00, 0xA0, 0x0F, 0x00, 0x00, 0x53, 0x00, 0x68, 0x00,
				0x69, 0x00, 0x70, 0x00, 0x30, 0x00, 0x34, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
				0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xD2, 0xBD, 0xD0, 0x2E,
				0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x07, 0x00, 0x04, 0x89, 0x09, 0x00, 0x40, 0x1F, 0x00, 0x00,
				0x53, 0x00, 0x68, 0x00, 0x69, 0x00, 0x70, 0x00, 0x30, 0x00, 0x38, 0x00, 0x00, 0x00, 0x00, 0x00,
				0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
				0xD2, 0xBD, 0xD0, 0x6A, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x03, 0x00, 0x14, 0x93, 0x09, 0x00,
				0xD0, 0x07, 0x00, 0x00, 0x53, 0x00, 0x68, 0x00, 0x69, 0x00, 0x70, 0x00, 0x30, 0x00, 0x32, 0x00,
				0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
				0x00, 0x00, 0x00, 0x00, 0xD2, 0xBD, 0xD0, 0x10, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x09, 0x00,
				0x5F, 0xE5, 0x15, 0x00, 0x20, 0x4E, 0x00, 0x00, 0x53, 0x00, 0x68, 0x00, 0x69, 0x00, 0x70, 0x00,
				0x32, 0x00, 0x30, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
				0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xD2, 0xBD, 0xD0, 0xB5, 0x00, 0x00, 0x00, 0x00,
				0x00, 0x01, 0x00, 0x01, 0xFF, 0xFF, 0xFF, 0xFF, 0xE8, 0x03, 0x00, 0x00, 0x53, 0x00, 0x68, 0x00,
				0x69, 0x00, 0x70, 0x00, 0x30, 0x00, 0x31, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
				0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xD2, 0xBD, 0xD0, 0x01,
				0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x0A, 0x00, 0x3E, 0x42, 0x07, 0x00, 0xB8, 0x0B, 0x00, 0x00,
				0x53, 0x00, 0x68, 0x00, 0x69, 0x00, 0x70, 0x00, 0x30, 0x00, 0x33, 0x00, 0x00, 0x00, 0x00, 0x00,
				0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
				0xD2, 0xBD, 0xD0, 0x1F, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x08, 0x00, 0x83, 0x95, 0x09, 0x00,
				0x58, 0x1B, 0x00, 0x00, 0x53, 0x00, 0x68, 0x00, 0x69, 0x00, 0x70, 0x00, 0x30, 0x00, 0x37, 0x00,
				0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
				0x00, 0x00, 0x00, 0x00, 0xD2, 0xBD, 0xD0, 0x5B, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x04, 0x00,
				0xBF, 0x9B, 0x09, 0x00, 0x10, 0x27, 0x00, 0x00, 0x53, 0x00, 0x68, 0x00, 0x69, 0x00, 0x70, 0x00,
				0x31, 0x00, 0x30, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
				0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xD2, 0xBD, 0xD0, 0x88, 0x00, 0x00, 0x00, 0x00,
				0x01, 0x00, 0x01, 0x00, 0xBF, 0xB3, 0x08, 0x00, 0x4B, 0x55, 0xE9, 0x57, 0x01, 0x00, 0x00, 0x00
			};

			byte[] data = new ArraySegment<byte>(shipPacket, PacketBase.HeaderSize, shipPacket.Length - PacketBase.HeaderSize).ToArray();

			// [Size:4][Type:1][SubType:1][Flag1:1][Flag2:1]
			PacketHeader header = Structure.ByteArrayToStructure<PacketHeader>(new ArraySegment<byte>(shipPacket, 0, PacketBase.HeaderSize).ToArray());
			PacketBase pkt = new PacketBase(header, data);

			// [Size:4][Type:1][SubType:1][Flag1:1][Flag2:1]
			Assert.True(pkt.Header.size == 0x0250);
			Assert.True(pkt.Header.type == 0x11);
			Assert.True(pkt.Header.subType == 0x3D);
			Assert.True(pkt.Header.flag1 == 0x04);
			Assert.True(pkt.Header.flag2 == 0x00);
			Assert.True(pkt.Payload.SequenceEqual(data));
		}

		#endregion

		#endregion Polaris.Lib

		#region Polaris.Server.Ship

		// TODO: Need integration testing for entire connection procedure
		[Fact]
		public void PolarisServer_Ship_Info_TestShipListPacket()
		{
			Dictionary<string, string>[] Ships =
			{
				new Dictionary<string, string>()
				{
					{ "ShipName", "Test1" },
					{ "IPAddress", "127.0.0.1" },
					{ "Port", "12100" },
					{ "Status", "Online" },
				},
				new Dictionary<string, string>()
				{
					{ "ShipName", "Test2" },
					{ "IPAddress", "127.0.0.1" },
					{ "Port", "12200" },
					{ "Status", "Online" },
				},
			};

			IPAddress addr = new IPAddress(new byte[] { 127, 0, 0, 1});

			Info.Instance.Initialize(addr.ToString(), 12000, Ships);

			int expectedSize = PacketBase.HeaderSize + Marshal.SizeOf<ShipEntry>() * Ships.Length + 12;
			byte[] buffer = new byte[expectedSize];

			using (var client = new TcpClient() )
			{
				client.Client.Connect(addr, 12000);
				client.Client.Receive(buffer);
				client.Close();
			}

			Info.Instance.Stop();

			PacketShipList shipList = new PacketShipList(buffer);
			PacketHeader Header = PacketBase.GeneratePacketHeader((uint)expectedSize, 0x11, 0x3D, 0x04, 0x00);

			Assert.True(Structure.StructureToByteArray(shipList.Header).SequenceEqual(Structure.StructureToByteArray(Header)), "ShipList packet header does not match expected value");

			using (BinaryReader br = new BinaryReader(new MemoryStream(shipList.Payload)))
			{
				Assert.True(shipList.SubXor(br.ReadUInt32()) == Ships.Length, "ShipList packet entry count does not match expected value");
				for(int i = 0; i < Ships.Length; i++)
				{
					byte[] entry = br.ReadBytes(Marshal.SizeOf<ShipEntry>());
					ShipEntry s = Structure.ByteArrayToStructure<ShipEntry>(entry);
					Assert.True(new IPAddress(s.IP).ToString() == Ships[i]["IPAddress"], $"ShipList Packet ShipEntry IP @ {i} does not match expected value");
					Assert.True(s.ShipName == Ships[i]["ShipName"], $"ShipList Packet ShipEntry ShipName @ {i} does not match expected value");
					Assert.True(s.Status == (ShipStatus)Enum.Parse(typeof(ShipStatus),Ships[i]["Status"]), $"ShipList Packet ShipEntry ShipName @ {i} does not match expected value");
				}
			}
		}

		#endregion

	}
}