package com.diddyholz.holztools

import androidx.fragment.app.Fragment
import androidx.fragment.app.FragmentManager
import androidx.fragment.app.FragmentStatePagerAdapter

class ModeCollectionPagerAdapter(fm: FragmentManager) : FragmentStatePagerAdapter(fm)
{
    override fun getCount(): Int  = MainActivity.TotalModes.toInt()

    override fun getItem(i: Int): Fragment {

        var fragment: Fragment = BlankFragment()

        when (i.toByte())
        {
            MainActivity.ModeStatic -> fragment = ModeStaticFragment()
            MainActivity.ModeCycle -> fragment = ModeCycleFragment()
            MainActivity.ModeRainbow -> fragment = ModeRainbowFragment()
            MainActivity.ModeLightning -> fragment = ModeLightningFragment()
            MainActivity.ModeOverlay -> fragment = ModeOverlayFragment()
            MainActivity.ModeSpinner -> fragment = ModeSpinnerFragment()
        }

        return fragment
    }

    override fun getPageTitle(position: Int): CharSequence
    {
        var modeName = ""

        when(position.toByte())
        {
            MainActivity.ModeStatic -> modeName = "Static"
            MainActivity.ModeCycle -> modeName = "Cycle"
            MainActivity.ModeRainbow -> modeName = "Rainbow"
            MainActivity.ModeLightning -> modeName = "Lightning"
            MainActivity.ModeOverlay -> modeName = "Color Overlay"
            MainActivity.ModeSpinner -> modeName = "Color Spinner"

            else -> {
                modeName = "OBJECT ${(position + 1)}"
            }
        }

        return modeName
    }
}