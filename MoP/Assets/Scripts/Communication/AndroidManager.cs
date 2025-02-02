﻿using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class AndroidManager : MonoBehaviour {
#if UNITY_ANDROID
	AndroidJavaObject _activity;
	AndroidJavaClass _class;

	DateTime lastPtCmd;

	const bool DEBUG = false;

	void Awake () {
		Debug.Log ("[AndroidManager:Awake]");
		AndroidJavaClass jc = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
		_activity = jc.GetStatic<AndroidJavaObject>("currentActivity");
		if (_activity == null)
			Debug.Log ("AndroidActivity is null");
		_activity.Call ("Echo", "Hello");
		lastPtCmd = DateTime.Now;
	}

	// Use this for initialization
	void Start () {
		Debug.Log ("[AndroidManager:Start]");
	}
	
	// Update is called once per frame
	int count;
	void Update () {
//		count++;
//		_activity.Call("Echo", count.ToString ());
	}

	void Echo (string message) {
		Debug.Log ("[Unity]Get echo message: " + message);
	}

	public void SearchBTDevice () {
		Debug.Log ("[AndroidManager:SearchBTDevices]");
		List<string> devices = GetDevice ();
		if (devices == null)
			Debug.Log ("Start searching...");
		else {
			foreach (string device in devices)
				Debug.Log ("Found device : " + device);
		}
	}
#if true
	private List<string> _bluetoothDeviceNameList = new List<string>();
	private List<byte> _recvBuffer = new List<byte>();

	private bool _isBluetoothConnected;
	public int BluetoothConnectingState;
//	private int _receivedData = -1;
//	private bool _isReceivedMessage;
	//------------------------------------------
	
	#region RobotCommunicator Interface Methods
	public bool IsBluetoothDeviceNameListDone
	{
		get;
		set;
	}
	
	public List<string> GetDevice()
	{
		if (IsBluetoothDeviceNameListDone == false)
		{
			_SearchDevice();
			
			return null;
		}
		else
		{
			return _bluetoothDeviceNameList;
		}
	}
	
	public bool IsConnected()
	{
		return _isBluetoothConnected;
	}
	
	public void ConnectDevice(string name)
	{
		BluetoothConnectingState = 2;
		_ConnectBluetooth(name);
	}
	
	public void DisconnectDevice()
	{
		BluetoothConnectingState = 1;
		_DisconnectBluetooth();
	}
	
	private String ByteArrayToHexString(byte[] bytes)
	{
		return BitConverter.ToString(bytes).Replace("-", "");
	}
	
	static byte[] HexStringToByteArray(string hexString)
	{
		if (hexString == null)
			throw new ArgumentNullException("hexString");
		
		if ((hexString.Length & 1) != 0)
			throw new ArgumentOutOfRangeException("hexString", hexString, "hexString must contain an even number of characters.");
		
		byte[] result = new byte[hexString.Length/2];
		for (int i = 0; i < hexString.Length; i += 2)
			result[i/2] = byte.Parse(hexString.Substring(i, 2), System.Globalization.NumberStyles.HexNumber);
		
		return result;
	}
	
	public void Send(byte[] buff)
	{
//		Debug.Log ("[AndroidManager::Send] " + ByteArrayToHexString(buff));
		try {
			_SendBTMessage(ByteArrayToHexString(buff));
		}catch (Exception e) {
			Debug.Log (e.ToString ());
		}
	}
	
	public int Recv(ref byte[] buff, int len)
	{
		int count = _recvBuffer.Count;
		int copyLength = (len < count) ? len : count;
		buff = new byte[copyLength];
		if (count > 1) {
			Buffer.BlockCopy(_recvBuffer.ToArray(), 0, buff, 0, copyLength);
			_recvBuffer.RemoveRange(0, copyLength);
		}
//		Debug.Log ("[AndroidManager::Recv] Copy " + copyLength + " remain " + _recvBuffer.Count.ToString ());
		return copyLength;
	}
	
	public int Length
	{
		get {
			return _recvBuffer.Count;
		}
	}
	#endregion

	#region Event Methods
	void BluetoothDevice(string deviceName)
	{
		Debug.Log ("[AndroidManager::BluetoothDevice] : " + deviceName);
		if (deviceName == "END")
		{
			IsBluetoothDeviceNameListDone = true;
			return;
		}
		
		_bluetoothDeviceNameList.Add(deviceName);
	}
	
	void BluetoothConnectState(string signal)
	{
		Debug.Log ("[AndroidManager::BluetoothConnectState] " + signal);
		
		if (signal == "BT_STATE_CONNECTED") {
			BluetoothConnectingState = 3;
			_isBluetoothConnected = true;
		} else if (signal == "BT_STATE_NOTCONNECTED") {
			BluetoothConnectingState = 1;
			_isBluetoothConnected = false;
		}
	}
	
	void BluetoothData(string readData)
	{
		byte[] recvBuffer = HexStringToByteArray(readData);
		for (int i = 0 ; i < readData.Length/2 ; i++)
			_recvBuffer.Add(recvBuffer[i]);
		if (DEBUG)
			Debug.Log("[AndroidManager::BluetoothData] " + readData);
	}

	float pan = 0f;
	float tilt = 0f;
	float PAN_STEP = 0.01f;
	float PAN_MIN = -1f;
	float PAN_MAX = 1f;
	float TLT_STEP = 0.02f;
	float TLT_MIN = -1f;
	float TLT_MAX = 1f;
	void FacePos (string pos)
	{
		if (DateTime.Now.Subtract (lastPtCmd).TotalMilliseconds < 50)
			return;
		lastPtCmd = DateTime.Now;

		string[] pt = pos.Split (',');
		int posX = int.Parse (pt [0]);
		int posY = int.Parse (pt [1]);

		if (Math.Abs (posX) > 100) {
			if (posX < -100)
				pan -= PAN_STEP;
			else if (posX > 100)
				pan += PAN_STEP;
			if (pan < PAN_MIN)
				pan = PAN_MIN;
			if (pan > PAN_MAX)
				pan = PAN_MAX;
		}

		if (Math.Abs (posY) > 100) {
			if (posY < -100)
				tilt -= TLT_STEP;
			else if (posY > 100)
				tilt += TLT_STEP;
			if (tilt < TLT_MIN)
				tilt = TLT_MIN;
			if (tilt > TLT_MAX)
				tilt = TLT_MAX;
		}

		StopCoroutine ("PanTiltCenter");
		RobotManager.Instance.PanTilt(-pan, tilt);
//		Debug.Log ("[AndroidManager::FacePos] x: " + pt [0] + ", y: " + pt [1]);
		StartCoroutine ("PanTiltCenter");
	}
	#endregion

	float RTN_STEP = 0.05f;
	IEnumerator PanTiltCenter() 
	{
		yield return new WaitForSeconds(3f);
		while (Math.Abs (pan) + Math.Abs (tilt) > 0.11f) {
			pan = (pan > 0) ? pan-RTN_STEP : pan+RTN_STEP;
			tilt = (tilt > 0) ? tilt-RTN_STEP : tilt+RTN_STEP;
			RobotManager.Instance.PanTilt(-pan, tilt);
			yield return new WaitForSeconds (0.1f);
		}
		RobotManager.Instance.PanTilt (0f, 0f);
	}

	#region Bluetooth private Methods
	private void _SendBTMessage(String message)
	{
		_activity.Call("SendMessage", message);
	}
	
//	private int _ReceiveBTMessage()
//	{
//		_isReceivedMessage = false;
//		return _receivedData;
//	}
//	
	private void _SearchDevice()
	{
		_bluetoothDeviceNameList.Clear();
		IsBluetoothDeviceNameListDone = false;
		_activity.Call("SearchBluetoothDevice");
	}
	
	private void _ConnectBluetooth(string deviceName)
	{
#if UNITY_EDITOR
#else
		_activity.Call("ConnectDevice", deviceName);
#endif
	}
	
	private void _DisconnectBluetooth()
	{
		#if UNITY_EDITOR
		#else
		_activity.Call("DisconnectBluetooth");
		#endif
		_isBluetoothConnected = false;
	}
	#endregion
	#endif
#else
	public void ConnectDevice(string name) { }
	public void DisconnectDevice() { }
	public bool IsConnected() {return false;}
	public List<string> GetDevice() {return null;}

	public void Send(byte[] buff) { }
	
	public int Recv(ref byte[] buff, int len)
	{
		return 0;
	}
#endif
	private static AndroidManager _instance = null;
	public static AndroidManager Instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = FindObjectOfType(typeof(AndroidManager)) as AndroidManager;
				if (_instance == null)
					Debug.LogError("There needs to be one active BehaviorManager script on a GameObject in your scene.");
				
			}
			return _instance;
		}
	}
}
