package com.diddyholz.holztools

import android.app.AlertDialog
import android.os.Bundle
import androidx.fragment.app.Fragment
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import kotlinx.android.synthetic.main.fragment_about.*

class AboutFragment : Fragment() {

    override fun onCreateView(
        inflater: LayoutInflater, container: ViewGroup?,
        savedInstanceState: Bundle?
    ): View? {
        // Inflate the layout for this fragment
        return inflater.inflate(R.layout.fragment_about, container, false)
    }

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)

        licenses_btn.setOnClickListener {
            AlertDialog.Builder(context)
                .setTitle("Licenses")
                .setMessage(getString(R.string.license_mit) + "\n\n\n\n" + getString(R.string.license_apache))
                .setPositiveButton("Ok", null)
                .show()
        }
    }
}
