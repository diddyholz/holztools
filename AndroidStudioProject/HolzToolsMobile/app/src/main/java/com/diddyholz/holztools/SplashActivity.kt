package com.diddyholz.holztools

import android.content.Intent
import android.net.Uri
import android.os.Bundle
import android.widget.Toast
import androidx.appcompat.app.AlertDialog
import androidx.appcompat.app.AppCompatActivity
import androidx.preference.PreferenceManager
import kotlinx.android.synthetic.main.activity_splash.*
import kotlinx.coroutines.*
import org.jsoup.Jsoup
import java.net.InetAddress

class SplashActivity : AppCompatActivity()
{
    override fun onCreate(savedInstanceState: Bundle?)
    {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_splash)
        setTheme(R.style.PreferenceStyle)

        if(savedInstanceState != null)
            return

        CoroutineScope(Dispatchers.Default).launch {
            with(PreferenceManager.getDefaultSharedPreferences(this@SplashActivity))
            {
                MainActivity.notifyOfUpdates = getBoolean(PreferenceKeys.settingsNotifyOfUpdates, true)
                MainActivity.serverPort = getString(PreferenceKeys.settingsDefaultPortPreference, "39769")!!.toInt()
                MainActivity.connectTimeout = getString(PreferenceKeys.settingsConnectionTimeoutPreference, "150")!!.toInt()
            }

            //check for an application update
            if(MainActivity.isNetworkOnline(this@SplashActivity))
            {
                CoroutineScope(Dispatchers.Main).launch {
                    statusTextView.text = "Checking for updates"
                }
                val updateString = Jsoup.connect(MainActivity.updatePasteBin).get().text()

                var tmp = true

                if (updateString.split('|')[0] != getString(R.string.app_version))
                {
                    CoroutineScope(Dispatchers.Main).launch {
                        AlertDialog.Builder(this@SplashActivity)
                            .setTitle("Update Available")
                            .setTitle("Update Available")
                            .setMessage("A new update is available. Do you want to download it now? You will be redirected to your browser, where the .apk file will be downloaded.")
                            .setPositiveButton(android.R.string.yes) { dialog, which ->
                                val browserIntent = Intent(Intent.ACTION_VIEW, Uri.parse(updateString.split('|')[1]))
                                startActivity(browserIntent)
                            }
                            .setNegativeButton(android.R.string.no) { dialog, which ->
                                tmp = false
                            }
                            .show()
                    }

                    while (tmp)
                        Thread.sleep(100)
                }
            }

            CoroutineScope(Dispatchers.Main).launch { statusTextView.text = "Loading your data" }
            MainActivity.loadUserData(this@SplashActivity)

            if(MainActivity.isNetworkOnline(this@SplashActivity))
            {
                CoroutineScope(Dispatchers.Main).launch {
                    statusTextView.text = "Connecting to your LEDs"
                }
                var reachableAddresses = mutableListOf<InetAddress>()

                for (item in LedItem.allItems) {
                    // check if a connection to the saved ip can be established 3 times, if no then search ips for the correct led
                    if (item.ip.isEmpty())
                        continue

                    var canConnect = false

                    for (x in 0 until 3) {
                        canConnect = item.checkConnection()
                        delay(200);
                    }

                    if (canConnect)
                        continue

                    CoroutineScope(Dispatchers.Main).launch {
                        Toast.makeText(
                            this@SplashActivity,
                            "Cannot connect to ${item.customName}! The device may have received a new IP-address. Trying to find the new address.",
                            Toast.LENGTH_LONG
                        ).show()
                    }

                    if (reachableAddresses.isEmpty())
                        reachableAddresses = MainActivity.getReachableIPs(this@SplashActivity)

                    for (address in reachableAddresses) {
                        val response = MainActivity.sendGetRequest(
                            address.hostAddress,
                            item.tcpServerPort,
                            mutableListOf<HTTPAttribute>(
                                HTTPAttribute(
                                    "Command",
                                    MainActivity.TCPGETINFO
                                )
                            )
                        )

                        if (response == MainActivity.TCPCOULDNOTCONNECT.toString())
                            continue

                        val tmp = response!!.split('&')

                        // check the attributes in the response
                        for (arg in tmp) {
                            val argName = arg.split('=')[0]
                            val argValue = arg.split('=')[1]

                            when (argName) {
                                "Hostname" -> {
                                    if (item.hostname == argValue)
                                        item.ip = address.hostAddress
                                }
                            }
                        }
                    }
                }
            }

            CoroutineScope(Dispatchers.Main).launch {
                val activity = MainActivity()
                val intent = Intent(this@SplashActivity, activity.javaClass)

                startActivity(intent)
                finish()
            }
        }
    }
}