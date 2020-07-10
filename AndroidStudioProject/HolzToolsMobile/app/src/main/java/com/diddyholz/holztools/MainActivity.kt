package com.diddyholz.holztools

import android.content.Context
import android.content.Intent
import android.graphics.Color
import android.net.ConnectivityManager
import android.net.NetworkCapabilities
import android.net.wifi.WifiManager
import android.os.Bundle
import android.text.InputFilter
import android.view.Menu
import android.view.MenuItem
import android.view.View
import android.widget.Toast
import androidx.appcompat.app.ActionBarDrawerToggle
import androidx.appcompat.app.AppCompatActivity
import androidx.core.view.GravityCompat
import androidx.core.view.MenuCompat
import androidx.preference.PreferenceManager
import com.google.android.material.navigation.NavigationView
import kotlinx.android.synthetic.main.activity_main.*
import kotlinx.coroutines.*
import okhttp3.*
import okhttp3.HttpUrl.Companion.toHttpUrlOrNull
import java.math.BigInteger
import java.net.ConnectException
import java.net.InetAddress
import java.net.SocketTimeoutException
import java.nio.ByteOrder
import java.util.concurrent.TimeUnit

/*
* TODO:
* */

class MainActivity : AppCompatActivity(), NavigationView.OnNavigationItemSelectedListener
{
    companion object
    {
        const val updatePasteBin = "https://pastebin.com/raw/3eCaBXCf"

        //constants for the mode
        const val TotalModes:Byte = 6

        const val ModeStatic:Byte = 0
        const val ModeCycle:Byte = 1
        const val ModeRainbow:Byte = 2
        const val ModeLightning:Byte = 3
        const val ModeOverlay:Byte = 4
        const val ModeSpinner:Byte = 5

        // constants for tcp codes
        const val TCPGETINFO = "GETINFO"
        const val TCPSETLED = "SETLED"
        const val TCPREQUESTOK = 200
        const val TCPNOCONNECTEDLEDS = 300
        const val TCPINVALIDCOMMAND = 400
        const val TCPCOULDNOTCONNECT = 401
        const val TCPLEDNOTFOUND = 404

        // constants for led types
        const val LEDTYPE3PIN = 0
        const val LEDTYPE4PIN = 1

        const val blockCharacterSet = ",&=@"

        // settings
        var notifyOfUpdates = true
        var serverPort = 39769
        var accentColor = Color.parseColor("#C60000")
        var connectTimeout = 150
            set(value) {
                field = value
                client = OkHttpClient.Builder().connectTimeout(field.toLong(), TimeUnit.MILLISECONDS).build()
            }

        var selectedLedItem: LedItem? = null
        var client = OkHttpClient.Builder().connectTimeout(connectTimeout.toLong(), TimeUnit.MILLISECONDS).build()

        lateinit var activeMainActivity:MainActivity

        fun saveUserData(context: Context)
        {
            PreferenceManager.getDefaultSharedPreferences(context).edit().putInt(PreferenceKeys.userDataLedItemCountPreference, LedItem.allItems.size).apply()

            for((index, value) in LedItem.allItems.withIndex())
            {
                with(context.getSharedPreferences(PreferenceKeys.prefixUserDataSharedPreferences + index.toString(), Context.MODE_PRIVATE).edit())
                {
                    // save leditem properties
                    putInt(PreferenceKeys.userDataLedItemTypePreference, value.type.toInt())
                    putInt(PreferenceKeys.userDataLedItemCurrentModePreference, value.currentMode.toInt())
                    putInt(PreferenceKeys.userDataLedItemLedCountPreference, value.ledCount.toInt())
                    putInt(PreferenceKeys.userDataLedItemDPinPreference, value.dPin.toInt())
                    putInt(PreferenceKeys.userDataLedItemRPinPreference, value.rPin.toInt())
                    putInt(PreferenceKeys.userDataLedItemGPinPreference, value.gPin.toInt())
                    putInt(PreferenceKeys.userDataLedItemBPinPreference, value.bPin.toInt())
                    putInt(PreferenceKeys.userDataLedItemPortPreference, value.tcpServerPort)
                    putString(PreferenceKeys.userDataLedItemIPPreference, value.ip)
                    putString(PreferenceKeys.userDataLedItemHostNamePreference, value.hostname)
                    putString(PreferenceKeys.userDataLedItemCustomNamePreference, value.customName)
                    putString(PreferenceKeys.userDataLedItemHostLedNamePreference, value.hostLedName)
                    putBoolean(PreferenceKeys.userDataLedItemIsOnPreference, value.isOn)
                    putBoolean(PreferenceKeys.userDataLedItemUseAdvancedIPPreference, value.useAdvancedIpSettings)
                    putBoolean(PreferenceKeys.userDataLedItemIsConnectedToPCPreference, value.isConnectedToPC)

                    // save mode attributes
                    putInt(PreferenceKeys.userDataLedItemStaticBrightnessPreference, value.staticBrightness)
                    putInt(PreferenceKeys.userDataLedItemStaticColorPreference, value.staticColor)
                    putInt(PreferenceKeys.userDataLedItemCycleBrightnessPreference, value.cycleBrightness)
                    putInt(PreferenceKeys.userDataLedItemCycleSpeedPreference, value.cycleSpeed)
                    putInt(PreferenceKeys.userDataLedItemRainbowBrightnessPreference, value.rainbowBrightness)
                    putInt(PreferenceKeys.userDataLedItemRainbowSpeedPreference, value.rainbowSpeed)
                    putInt(PreferenceKeys.userDataLedItemLightningBrightnessPreference, value.lightningBrightness)
                    putInt(PreferenceKeys.userDataLedItemLightningColorPreference, value.lightningColor)
                    putInt(PreferenceKeys.userDataLedItemOverlaySpeedPreference, value.overlaySpeed)
                    putInt(PreferenceKeys.userDataLedItemOverlayDirectionPreference, value.overlayDirection)
                    putInt(PreferenceKeys.userDataLedItemSpinnerSpinnerColorPreference, value.spinnerSpinnerColor)
                    putInt(PreferenceKeys.userDataLedItemSpinnerSpinnerBrightnessPreference, value.spinnerColorBrightness)
                    putInt(PreferenceKeys.userDataLedItemSpinnerSpinnerLengthPreference, value.spinnerLength)
                    putInt(PreferenceKeys.userDataLedItemSpinnerBackgroundColorPreference, value.spinnerBackgroundColor)
                    putInt(PreferenceKeys.userDataLedItemSpinnerBackgroundBrightnessPreference, value.backgroundColorBrightness)

                    apply()
                }
            }
        }

        fun loadUserData(context: Context)
        {
            val ledItemCount = PreferenceManager.getDefaultSharedPreferences(context).getInt(PreferenceKeys.userDataLedItemCountPreference, 0)

            for (x in 0 until ledItemCount)
            {
                with(context.getSharedPreferences(PreferenceKeys.prefixUserDataSharedPreferences + x.toString(), Context.MODE_PRIVATE))
                {
                    val item = LedItem()

                    // load leditem properties
                    item.type = getInt(PreferenceKeys.userDataLedItemTypePreference, item.type.toInt()).toByte()
                    item.currentMode = getInt(PreferenceKeys.userDataLedItemCurrentModePreference, item.currentMode.toInt()).toByte()
                    item.ledCount = getInt(PreferenceKeys.userDataLedItemLedCountPreference, item.ledCount.toInt()).toByte()
                    item.dPin = getInt(PreferenceKeys.userDataLedItemDPinPreference, item.dPin.toInt()).toByte()
                    item.rPin = getInt(PreferenceKeys.userDataLedItemRPinPreference, item.rPin.toInt()).toByte()
                    item.gPin = getInt(PreferenceKeys.userDataLedItemGPinPreference, item.gPin.toInt()).toByte()
                    item.bPin = getInt(PreferenceKeys.userDataLedItemBPinPreference, item.bPin.toInt()).toByte()
                    item.tcpServerPort = getInt(PreferenceKeys.userDataLedItemPortPreference, item.tcpServerPort)
                    item.ip = getString(PreferenceKeys.userDataLedItemIPPreference, item.ip)!!
                    item.hostname = getString(PreferenceKeys.userDataLedItemHostNamePreference, item.hostname)!!
                    item.customName = getString(PreferenceKeys.userDataLedItemCustomNamePreference, item.customName)!!
                    item.hostLedName = getString(PreferenceKeys.userDataLedItemHostLedNamePreference, item.hostLedName)!!
                    item.isOn = getBoolean(PreferenceKeys.userDataLedItemIsOnPreference, item.isOn)
                    item.useAdvancedIpSettings = getBoolean(PreferenceKeys.userDataLedItemUseAdvancedIPPreference, item.useAdvancedIpSettings)
                    item.isConnectedToPC = getBoolean(PreferenceKeys.userDataLedItemIsConnectedToPCPreference, item.isConnectedToPC)

                    // load mode attributes
                    item.staticBrightness = getInt(PreferenceKeys.userDataLedItemStaticBrightnessPreference, item.staticBrightness)
                    item.staticColor = getInt(PreferenceKeys.userDataLedItemStaticColorPreference, item.staticColor)
                    item.cycleBrightness = getInt(PreferenceKeys.userDataLedItemCycleBrightnessPreference, item.cycleBrightness)
                    item.cycleSpeed = getInt(PreferenceKeys.userDataLedItemCycleSpeedPreference, item.cycleSpeed)
                    item.rainbowBrightness = getInt(PreferenceKeys.userDataLedItemRainbowBrightnessPreference, item.rainbowBrightness)
                    item.rainbowSpeed = getInt(PreferenceKeys.userDataLedItemRainbowSpeedPreference, item.rainbowSpeed)
                    item.lightningBrightness = getInt(PreferenceKeys.userDataLedItemLightningBrightnessPreference, item.lightningBrightness)
                    item.lightningColor = getInt(PreferenceKeys.userDataLedItemLightningColorPreference, item.lightningColor)
                    item.overlaySpeed = getInt(PreferenceKeys.userDataLedItemOverlaySpeedPreference, item.overlaySpeed)
                    item.overlayDirection = getInt(PreferenceKeys.userDataLedItemOverlayDirectionPreference, item.overlayDirection)
                    item.spinnerSpinnerColor = getInt(PreferenceKeys.userDataLedItemSpinnerSpinnerColorPreference, item.spinnerSpinnerColor)
                    item.spinnerColorBrightness = getInt(PreferenceKeys.userDataLedItemSpinnerSpinnerBrightnessPreference, item.spinnerColorBrightness)
                    item.spinnerLength = getInt(PreferenceKeys.userDataLedItemSpinnerSpinnerLengthPreference, item.spinnerLength)
                    item.spinnerBackgroundColor = getInt(PreferenceKeys.userDataLedItemSpinnerBackgroundColorPreference, item.spinnerBackgroundColor)
                    item.backgroundColorBrightness = getInt(PreferenceKeys.userDataLedItemSpinnerBackgroundBrightnessPreference, item.backgroundColorBrightness)
                }
            }
        }

        fun isNetworkOnline(context: Context): Boolean
        {
            val cm = context.getSystemService(Context.CONNECTIVITY_SERVICE) as ConnectivityManager

            val networkCapabilities = cm.getNetworkCapabilities(cm.activeNetwork) ?: return false

            return networkCapabilities.hasTransport(NetworkCapabilities.TRANSPORT_WIFI)
        }

        fun getOwnIp(context: Context): String
        {
            val wm = context.getSystemService(Context.WIFI_SERVICE) as WifiManager
            val connectionInfo = wm.connectionInfo
            val ipAddress = if (ByteOrder.nativeOrder().equals(ByteOrder.LITTLE_ENDIAN)) Integer.reverseBytes(connectionInfo.ipAddress) else connectionInfo.ipAddress

            return InetAddress.getByAddress(BigInteger.valueOf(ipAddress.toLong()).toByteArray()).hostAddress
        }

        fun getReachableIPs(context: Context): MutableList<InetAddress>
        {
            // get the device ip to check the local ip prefix
            val ownIP = getOwnIp(context)
            val prefix = ownIP.substring(0, ownIP.lastIndexOf(".") + 1)

            var reachableIPs = mutableListOf<InetAddress>()

            val maxCoroutineAmount = 20

            var runningCoroutines = 0

            //irritate through all ip addresses in this network with the obtained prefix
            for (i in 0..254)
            {
                // limit the amount of simultaneous coroutines
                while (runningCoroutines == maxCoroutineAmount)
                    runBlocking { delay(150) }

                runningCoroutines++

                CoroutineScope(Dispatchers.IO).launch {

                    val testIp = prefix + i.toString()
                    val address = InetAddress.getByName(testIp)
                    val reachable = address.isReachable(connectTimeout)

                    if (reachable && testIp != ownIP)
                        reachableIPs.add(address)

                    runningCoroutines--
                }
            }

            return reachableIPs
        }

        fun sendGetRequest(targetIp: String, targetPort: Int, attributes: MutableList<HTTPAttribute>): String?
        {
            try {
                var prefix = ""
                if(!targetIp.startsWith("http://") && !targetIp.startsWith("https://") && !targetIp.startsWith("tcp://"))
                    prefix = "http://"

                val urlBuilder = "$prefix$targetIp:$targetPort".toHttpUrlOrNull()!!.newBuilder()

                // add every attribute
                for (attr in attributes) {
                    urlBuilder.addQueryParameter(attr.attrName, attr.attrValue)
                }

                urlBuilder.port(targetPort)

                val url = urlBuilder.build().toString()

                val request: Request =
                    Request.Builder().url(url).addHeader("Connection", "close").build()

                val call: Call = client.newCall(request)

                val response: Response = call.execute()

                return response.body?.string()
            }
            catch (e: SocketTimeoutException)
            {
                return TCPCOULDNOTCONNECT.toString()
            }
            catch (e: ConnectException)
            {
                return TCPCOULDNOTCONNECT.toString()
            }
        }

        fun sendDataToPC(item: LedItem): Int?
        {
            val attrs = mutableListOf<HTTPAttribute>()

            var modeString = ""

            attrs.add(HTTPAttribute("Command", TCPSETLED))

            attrs.add(HTTPAttribute("LEDItem", item.hostLedName))

            // set the current mode as a string
            when(item.currentMode)
            {
                ModeStatic -> {
                    modeString = "Static"
                    attrs.add(HTTPAttribute("StaticBrightness", item.staticBrightness.toString()))
                    attrs.add(HTTPAttribute("StaticModeColor", "${Color.red(item.staticColor)},${Color.green(item.staticColor)},${Color.blue(item.staticColor)}"))
                }
                ModeCycle -> {
                    modeString = "Cycle"
                    attrs.add(HTTPAttribute("CycleBrightness", item.cycleBrightness.toString()))
                    attrs.add(HTTPAttribute("CycleSpeed", item.cycleSpeed.toString()))
                }
                ModeRainbow -> {
                    modeString = "Rainbow"
                    attrs.add(HTTPAttribute("RainbowBrightness", item.rainbowBrightness.toString()))
                    attrs.add(HTTPAttribute("RainbowSpeed", item.rainbowSpeed.toString()))
                }
                ModeLightning -> {
                    modeString = "Lightning"
                    attrs.add(HTTPAttribute("LightningBrightness", item.lightningBrightness.toString()))
                    attrs.add(HTTPAttribute("LightningModeColor", "${Color.red(item.lightningColor)},${Color.green(item.lightningColor)},${Color.blue(item.lightningColor)}"))
                }
                ModeOverlay -> {
                    modeString = "Color_Overlay"
                    attrs.add(HTTPAttribute("OverlaySpeed", item.overlaySpeed.toString()))
                    attrs.add(HTTPAttribute("OverlayDirection", item.overlayDirection.toString()))
                }
                ModeSpinner -> {
                    modeString = "Color_Spinner"
                    attrs.add(HTTPAttribute("SpinnerModeSpinnerColor", "${Color.red(item.spinnerSpinnerColor)},${Color.green(item.spinnerSpinnerColor)},${Color.blue(item.spinnerSpinnerColor)}"))
                    attrs.add(HTTPAttribute("SpinnerColorBrightness", item.spinnerColorBrightness.toString()))
                    attrs.add(HTTPAttribute("SpinnerSpeed", item.spinnerSpeed.toString()))
                    attrs.add(HTTPAttribute("SpinnerLength", item.spinnerLength.toString()))
                    attrs.add(HTTPAttribute("SpinnerModeBackgroundColor", "${Color.red(item.spinnerBackgroundColor)},${Color.green(item.spinnerBackgroundColor)},${Color.blue(item.spinnerBackgroundColor)}"))
                    attrs.add(HTTPAttribute("BackgroundColorBrightness", item.backgroundColorBrightness.toString()))
                }
                else -> {
                    modeString = "Static"
                    attrs.add(HTTPAttribute("StaticBrightness", item.staticBrightness.toString()))
                    attrs.add(HTTPAttribute("StaticModeColor", "${Color.red(item.staticColor)},${Color.green(item.staticColor)},${Color.blue(item.staticColor)}"))
                }
            }

            attrs.add(HTTPAttribute("LEDMode", modeString))

            if(!item.isOn)
                attrs.add(HTTPAttribute("IsOn", false.toString()))

            return sendGetRequest(item.ip, item.tcpServerPort, attrs)?.toInt()
        }

        fun sendDataToArduino(item: LedItem): Int?
        {
            val attrs = mutableListOf<HTTPAttribute>()

            //get the selected mode shortname and get the arguments
            var tmpMode = "STTC"

            var arg1 = 0
            var arg2 = 0
            var arg3 = 0

            var arg4 = 0
            var arg5 = 0
            var arg6 = 0

            var arg7 = 0
            var arg8 = 0
            var arg9 = 0

            //set the arguments for the usb message
            when (item.currentMode)
            {
                ModeStatic -> {
                    tmpMode = "STTC"

                    arg1 = Color.red(item.staticColor) * (item.staticBrightness / 255)
                    arg2 = Color.green(item.staticColor) * (item.staticBrightness / 255)
                    arg3 = Color.blue(item.staticColor) * (item.staticBrightness / 255)
                }

                ModeCycle -> {
                    tmpMode = "CYCL"

                    arg7 = item.cycleSpeed
                    arg9 = item.cycleBrightness
                }

                ModeRainbow -> {
                    tmpMode = "RNBW"

                    arg7 = item.rainbowSpeed
                    arg9 = item.rainbowBrightness
                }

                ModeLightning -> {
                    tmpMode = "LING"

                    arg1 = Color.red(item.lightningColor) * (item.lightningBrightness / 255)
                    arg2 = Color.green(item.lightningColor) * (item.lightningBrightness / 255)
                    arg3 = Color.blue(item.lightningColor) * (item.lightningBrightness / 255)
                }

                ModeOverlay -> {
                    tmpMode = "OVRL"

                    arg7 = item.overlaySpeed
                    arg8 = item.overlayDirection
                }

                ModeSpinner -> {
                    tmpMode = "SPIN"

                    arg1 = Color.red(item.spinnerSpinnerColor) * (item.spinnerColorBrightness / 255)
                    arg2 = Color.green(item.spinnerSpinnerColor) * (item.spinnerColorBrightness / 255)
                    arg3 = Color.blue(item.spinnerSpinnerColor) * (item.spinnerColorBrightness / 255)

                    arg4 = Color.red(item.spinnerBackgroundColor) * (item.backgroundColorBrightness / 255)
                    arg5 = Color.green(item.spinnerBackgroundColor) * (item.backgroundColorBrightness / 255)
                    arg6 = Color.blue(item.spinnerBackgroundColor) * (item.backgroundColorBrightness / 255)

                    arg7 = item.spinnerSpeed
                    arg9 = item.spinnerLength
                }
            }

            if (!item.isOn)
            {
                //change the mode to off
                tmpMode = "TOFF"
            }

            var pins = ""

            //get the pins
            if (item.type == LEDTYPE3PIN.toByte())
            {
                pins = item.dPin.toString().padStart(2, '0') + item.ledCount.toString().padStart(4, '0')
            }
            else if (item.type == LEDTYPE4PIN.toByte())
            {
                pins = item.rPin.toString().padStart(2, '0') + item.gPin.toString().padStart(2, '0') + item.bPin.toString().padStart(2, '0')
            }

            //generate the usb message:     #MODE(4)ISMUSIC(1)TYPE(1)PINS(6)COLOR/ARGS(3 x 3)ID(2)\\n
            val message = "#${tmpMode}0${item.type}$pins${arg1.toString().padStart(3, '0')}${arg2.toString().padStart(3, '0')}" +
                    "${arg3.toString().padStart(3, '0')}${arg4.toString().padStart(3, '0')}${arg5.toString().padStart(3, '0')}" +
                    "${arg6.toString().padStart(3, '0')}${arg7.toString().padStart(3, '0')}${arg8.toString().padStart(3, '0')}" +
                    "${arg9.toString().padStart(3, '0')}${item.id}\\n"


            attrs.add(HTTPAttribute("Command", message))

            return sendGetRequest(item.ip, item.tcpServerPort, attrs)?.toInt()
        }

        val nameFilter =
            InputFilter { source, start, end, dest, dstart, dend ->
                if (source != null && blockCharacterSet.contains("" + source)) {
                    ""
                } else null
            }
    }

    val selectModeFragmentTag = "FRAGMENT_SELECTMODE"

    override fun onCreate(savedInstanceState: Bundle?)
    {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_main)

        activeMainActivity = this

        setSupportActionBar(toolbar)

        val drawerToggle = ActionBarDrawerToggle(
            this,
            mainDrawerLayout,
            toolbar,
            R.string.navigation_drawer_open,
            R.string.navigation_drawer_close
        )
        mainDrawerLayout.addDrawerListener(drawerToggle)
        drawerToggle.syncState()
        MenuCompat.setGroupDividerEnabled(navView.menu, true)

        navView.setNavigationItemSelectedListener(this)

        if (!isNetworkOnline(this))
            noConnectionAlert.visibility = View.VISIBLE

        // create a menu item for all leds
        for(ledItem in LedItem.allItems)
        {
            ledItem.createNavViewMenuItem()
        }

        if(savedInstanceState == null)
        {
            supportFragmentManager.beginTransaction().replace(R.id.mainFragmentContainer, NoLedsFragment()).commit()

            if(LedItem.allItems.size > 0)
            {
                setSelectedLedItem(LedItem.allItems[0])
                navView.setCheckedItem(selectedLedItem!!.menuItemId)
            }
        }
        else
        {
            when (savedInstanceState.getSerializable("selected_tab"))
            {
                "about" -> {
                    supportFragmentManager.beginTransaction().replace(R.id.mainFragmentContainer, AboutFragment()).commit()
                    toolbar.title = "About"
                }
                "settings" -> {
                    supportFragmentManager.beginTransaction().replace(R.id.mainFragmentContainer, SettingsFragment()).commit()
                    toolbar.title = "Settings"
                }
                "ledItem" -> {
                    if(selectedLedItem != null)
                    {
                        navView.setCheckedItem(selectedLedItem!!.menuItemId)

                        toolbar.title = selectedLedItem!!.customName
                    }
                }
            }
        }
    }

    override fun onSaveInstanceState(outState: Bundle) {
        super.onSaveInstanceState(outState)

        outState.putSerializable("selected_tab", when (toolbar.title) {
            "About" -> "about"
            "Settings" -> "settings"
            else -> "ledItem"
        })
    }

    fun setToolbarMenuVisibility(visible: Boolean)
    {
        toolbar.menu.findItem(R.id.menuLedItemProperties).isVisible = visible
        toolbar.menu.findItem(R.id.menuPower).isVisible = visible
    }

    fun setSelectedLedItem(item: LedItem)
    {
        selectedLedItem = item

        // set every mode attribute
        with(PreferenceManager.getDefaultSharedPreferences(this).edit())
        {
            putInt(PreferenceKeys.modeStaticColorPreference, selectedLedItem!!.staticColor)
            putInt(PreferenceKeys.modeStaticBrightnessPreference, selectedLedItem!!.staticBrightness)
            putInt(PreferenceKeys.modeCycleBrightnessPreference, selectedLedItem!!.cycleBrightness)
            putInt(PreferenceKeys.modeCycleSpeedPreference, 50 - selectedLedItem!!.cycleSpeed)
            putInt(PreferenceKeys.modeRainbowBrightnessPreference, selectedLedItem!!.rainbowBrightness)
            putInt(PreferenceKeys.modeRainbowSpeedPreference, 50 - selectedLedItem!!.rainbowSpeed)
            putInt(PreferenceKeys.modeLightningColorPreference, selectedLedItem!!.lightningColor)
            putInt(PreferenceKeys.modeLightningBrightnessPreference, selectedLedItem!!.lightningBrightness)
            putInt(PreferenceKeys.modeOverlaySpeedPreference, 50 - selectedLedItem!!.overlaySpeed)
            putString(PreferenceKeys.modeOverlayDirectionPreference, selectedLedItem!!.overlayDirection.toString())
            putInt(PreferenceKeys.modeSpinnerSpinnerColorPreference, selectedLedItem!!.spinnerSpinnerColor)
            putInt(PreferenceKeys.modeSpinnerSpinnerBrightnessPreference, selectedLedItem!!.spinnerColorBrightness)
            putInt(PreferenceKeys.modeSpinnerSpinnerSpeedPreference, 100 - selectedLedItem!!.spinnerSpeed)
            putInt(PreferenceKeys.modeSpinnerSpinnerLengthPreference, selectedLedItem!!.spinnerLength)
            putInt(PreferenceKeys.modeSpinnerBackgroundColorPreference, selectedLedItem!!.spinnerBackgroundColor)
            putInt(PreferenceKeys.modeSpinnerBackgroundBrightnessPreference, selectedLedItem!!.backgroundColorBrightness)

            apply()
        }

        if(item.isOn)
        {
            supportFragmentManager.beginTransaction().replace(R.id.mainFragmentContainer, SelectModeFragment(), selectModeFragmentTag).commitAllowingStateLoss()

            // set the correct mode
            (supportFragmentManager.findFragmentByTag(selectModeFragmentTag) as? SelectModeFragment)?.setViewPagerItem(selectedLedItem!!.currentMode.toInt())
        }
        else
        {
            supportFragmentManager.beginTransaction().replace(R.id.mainFragmentContainer, LedIsOffFragment(), selectModeFragmentTag).commitAllowingStateLoss()
        }

        toolbar.title = item.customName
    }

    override fun onCreateOptionsMenu(menu: Menu?): Boolean
    {
        menuInflater.inflate(R.menu.menu_toolbar, menu)

        if(selectedLedItem == null || toolbar.title == "About" || toolbar.title == "Settings")
            setToolbarMenuVisibility(false)

        return true
    }

    override fun onOptionsItemSelected(item: MenuItem): Boolean
    {
        if(item.itemId == R.id.menuLedItemProperties) {
            if (selectedLedItem == null)
                return false

            val newIntent = Intent(this, LedPropertiesActivity().javaClass)
            newIntent.putExtra("LedItemName", selectedLedItem!!.customName)
            startActivity(newIntent)
        }
        else if(item.itemId == R.id.menuPower)
        {
            if(selectedLedItem == null)
                return false

            selectedLedItem!!.isOn = !selectedLedItem!!.isOn

            if(selectedLedItem!!.isConnectedToPC)
                sendDataToPC(selectedLedItem!!)

            if(selectedLedItem!!.isOn)
                supportFragmentManager.beginTransaction().replace(R.id.mainFragmentContainer, SelectModeFragment(), selectModeFragmentTag).commit()
            else
                supportFragmentManager.beginTransaction().replace(R.id.mainFragmentContainer, LedIsOffFragment()).commit()
        }

        return true
    }

    override fun onNavigationItemSelected(item: MenuItem): Boolean
    {
        //choose what happens when a menuitem is pressed
        when(item.itemId)
        {
            R.id.menuSettings -> {
                supportFragmentManager.beginTransaction().replace(R.id.mainFragmentContainer, SettingsFragment()).commit()
                toolbar.title = "Settings"
                setToolbarMenuVisibility(false)
            }
            R.id.menuAbout -> {
                supportFragmentManager.beginTransaction().replace(R.id.mainFragmentContainer, AboutFragment()).commit()
                toolbar.title = "About"
                setToolbarMenuVisibility(false)
            }
            R.id.menuAddItem -> {
                LedItem()
                supportFragmentManager.beginTransaction().replace(R.id.mainFragmentContainer, SelectModeFragment(), selectModeFragmentTag).commit()
                toolbar.title = selectedLedItem!!.customName
                setToolbarMenuVisibility(true)

                saveUserData(this)
            }
            else -> {
                //show the modeSelectFragment if an LEDItem is clicked
                supportFragmentManager.beginTransaction().replace(R.id.mainFragmentContainer, SelectModeFragment(), selectModeFragmentTag).commit()

                var foundLedItem = false

                //set the selected ledItem
                for(leditem in LedItem.allItems)
                {
                    if(leditem.customName == item.title)
                    {
                        setSelectedLedItem(leditem)
                        foundLedItem = true
                    }
                }

                //display a toast if the leditem couldn't be found
                if(foundLedItem) {
                    toolbar.title = selectedLedItem!!.customName
                    setToolbarMenuVisibility(true)
                } else {
                    Toast.makeText(this, "Couldn't find LedItem!", Toast.LENGTH_SHORT).show()
                }
            }
        }

        mainDrawerLayout.closeDrawer(GravityCompat.START)
        return true
    }

    override fun onBackPressed()
    {
        if(mainDrawerLayout.isDrawerOpen(GravityCompat.START))
            mainDrawerLayout.closeDrawer(GravityCompat.START)
        else
            super.onBackPressed()
    }
}