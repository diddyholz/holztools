package com.diddyholz.holztools

import android.Manifest.permission.ACCESS_FINE_LOCATION
import android.app.AlertDialog
import android.bluetooth.BluetoothAdapter
import android.bluetooth.BluetoothDevice
import android.bluetooth.BluetoothSocket
import android.content.pm.PackageManager.PERMISSION_DENIED
import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.Toast
import androidx.activity.result.contract.ActivityResultContracts
import androidx.fragment.app.Fragment
import androidx.preference.PreferenceManager
import kotlinx.android.synthetic.main.fragment_configure_esp.*
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch


class ConfigureEspFragment : Fragment() {
    var socket: BluetoothSocket? = null

    override fun onCreateView(
        inflater: LayoutInflater, container: ViewGroup?,
        savedInstanceState: Bundle?
    ): View? {
        // Inflate the layout for this fragment
        return inflater.inflate(R.layout.fragment_configure_esp, container, false)
    }

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)

        if(requireContext().checkSelfPermission(ACCESS_FINE_LOCATION) == PERMISSION_DENIED)
        {
            if(shouldShowRequestPermissionRationale(ACCESS_FINE_LOCATION))
                AlertDialog.Builder(context)
                    .setTitle("Location permission")
                    .setMessage("The application needs the location permission to scan all nearby WiFi networks. If you don't want to use this feature, you can deny the permission, but you will not be able to configure your ESP32.")
                    .setPositiveButton("Ok", null)
                    .show()

            val requestPermissionLauncher = registerForActivityResult(
                ActivityResultContracts.RequestPermission()
            ) { isGranted: Boolean ->
                    if (isGranted)
                    {
                    }
                    else
                    {
                        childFragmentManager.beginTransaction().replace(
                            R.id.configureEspFragmentContainer,
                            NoLocationPermissionFragment()
                        ).commit()
                    }
                }

            requestPermissionLauncher.launch(ACCESS_FINE_LOCATION)
        }

        floatingSendButton.setOnClickListener {
            floatingSendButton.hide()

            val ssid = PreferenceManager.getDefaultSharedPreferences(context).getString(
                PreferenceKeys.configureEspNetworkSSIDPreference,
                "NO_SSID_SELECTED"
            )
            val password = PreferenceManager.getDefaultSharedPreferences(context).getString(
                PreferenceKeys.configureEspNetworkPasswordPreference,
                "NO_PASS_SELECTED"
            )
            val btDevice = PreferenceManager.getDefaultSharedPreferences(context).getString(
                PreferenceKeys.configureEspBluetoothPreference,
                "NO_BT_SELECTED"
            )

            if(ssid == "NO_SSID_SELECTED")
            {
                Toast.makeText(context, "Please select a valid network!", Toast.LENGTH_LONG).show()
                return@setOnClickListener
            }

            if(password == "NO_PASS_SELECTED")
            {
                Toast.makeText(context, "Please select a valid password!", Toast.LENGTH_LONG).show()
                return@setOnClickListener
            }

            if(btDevice == "NO_BT_SELECTED")
            {
                Toast.makeText(
                    context,
                    "Please select a valid Bluetooth device!",
                    Toast.LENGTH_LONG
                ).show()
                return@setOnClickListener
            }

            val btAdapter = BluetoothAdapter.getDefaultAdapter()

            if(btAdapter == null)
            {
                Toast.makeText(context, "This device does not support Bluetooth", Toast.LENGTH_LONG).show()
                return@setOnClickListener
            }

            if(!btAdapter.isEnabled)
            {
                Toast.makeText(context, "Bluetooth is disabled", Toast.LENGTH_LONG).show()
                return@setOnClickListener
            }

            val pairedDevices: Set<BluetoothDevice> = btAdapter.bondedDevices
            var device: BluetoothDevice? = null

            for(bt in pairedDevices)
            {
                if(bt.name == btDevice)
                {
                    device = bt
                    break
                }
            }

            if(device == null)
            {
                Toast.makeText(
                    context,
                    "Please select a valid Bluetooth device!",
                    Toast.LENGTH_LONG
                ).show()
                return@setOnClickListener
            }

            init(device)

            val message = "$ssid|$password\\n"

            if(socket!!.isConnected) {
                socket!!.outputStream.write(message.toByteArray())

                connectingToNetworkLayout.visibility = View.VISIBLE
                connectingToNetworkText.text = "Connecting to $ssid"

                var text = ""
                var ipAddress = ""

                CoroutineScope(Dispatchers.Default).launch {
                    while (true)
                    {
                        val buffer = ByteArray(1)
                        val length: Int = socket!!.inputStream.read(buffer)

                        text += String(buffer, 0, length)

                        if(text.contains("\n"))
                        {
                            if(text == "#YESCONNECT\n") {
                                CoroutineScope(Dispatchers.Main).launch {
                                    Toast.makeText(context, "Succesfully connected\nIP-Address: $ipAddress", Toast.LENGTH_LONG).show()
                                    connectingToNetworkLayout.visibility = View.GONE
                                    floatingSendButton.show()
                                }
                                socket!!.close()
                                return@launch
                            } else if(text == "#NOCONNECT\n") {
                                CoroutineScope(Dispatchers.Main).launch {
                                    Toast.makeText(context, "ESP32 cannot connect to network. Maybe try again", Toast.LENGTH_LONG).show()
                                    connectingToNetworkLayout.visibility = View.GONE
                                    floatingSendButton.show()
                                }
                                socket!!.close()
                                return@launch
                            } else if(text.contains("\r")){
                                // ignore
                            } else {
                                ipAddress = text.substring(1, text.indexOf("\n"))
                            }

                            text = ""
                        }
                    }
                }
            }
        }

        childFragmentManager.beginTransaction().replace(
            R.id.configureEspFragmentContainer,
            ConfigureEspPreferenceFragment()
        ).commit()

        if(!PreferenceManager.getDefaultSharedPreferences(context).getBoolean(
                PreferenceKeys.configureEspDialogShownPreference,
                false
            ))
            {
            AlertDialog.Builder(context)
                .setTitle("Info")
                .setMessage(getString(R.string.configure_esp_info))
                .setPositiveButton("Ok", null)
                .show()

            PreferenceManager.getDefaultSharedPreferences(context).edit().putBoolean(
                PreferenceKeys.configureEspDialogShownPreference,
                true
            ).apply()
        }
    }

    private fun init(device: BluetoothDevice) {
        val blueAdapter = BluetoothAdapter.getDefaultAdapter()

        if(blueAdapter == null) {
            Toast.makeText(context, "This device does not support bluetooth", Toast.LENGTH_LONG).show()
            floatingSendButton.show()
            return
        }

        if (!blueAdapter.isEnabled) {
            Toast.makeText(context, "Bluetooth is disabled", Toast.LENGTH_LONG).show()
            floatingSendButton.show()
            return
        }

        val uuids = device.uuids
        socket = device.createRfcommSocketToServiceRecord(uuids[0].uuid)

        try
        {
            socket!!.connect()
        }
        catch (ex: Exception)
        {
            Toast.makeText(context, "Unable to connect to ${device.name}", Toast.LENGTH_LONG).show()
            floatingSendButton.show()
        }
    }
}