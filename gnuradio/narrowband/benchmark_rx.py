#!/usr/bin/env python
#
# Copyright 2010,2011 Free Software Foundation, Inc.
# 
# This file is part of GNU Radio
# 
# GNU Radio is free software; you can redistribute it and/or modify
# it under the terms of the GNU General Public License as published by
# the Free Software Foundation; either version 3, or (at your option)
# any later version.
# 
# GNU Radio is distributed in the hope that it will be useful,
# but WITHOUT ANY WARRANTY; without even the implied warranty of
# MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
# GNU General Public License for more details.
# 
# You should have received a copy of the GNU General Public License
# along with GNU Radio; see the file COPYING.  If not, write to
# the Free Software Foundation, Inc., 51 Franklin Street,
# Boston, MA 02110-1301, USA.
# 

from gnuradio import gr, gru
from gnuradio import eng_notation
from gnuradio.eng_option import eng_option
from optparse import OptionParser
from gnuradio.filter import firdes
from gnuradio.gr import firdes
from gnuradio import blocks
from gnuradio import eng_notation
from gnuradio import filter

# From gr-digital
from gnuradio import digital

# from current dir
from receive_path import receive_path
from uhd_interface import uhd_receiver

import struct
import sys

#import os
#print os.getpid()
#raw_input('Attach and press enter: ')

class my_top_block(gr.top_block):
    def __init__(self, demodulator, rx_callback0, rx_callback1, options):
        gr.top_block.__init__(self)

        if(options.rx_freq is not None):
            # Work-around to get the modulation's bits_per_symbol
            args = demodulator.extract_kwargs_from_options(options)
            symbol_rate = options.bitrate / demodulator(**args).bits_per_symbol()

            self.source = uhd_receiver(options.args, symbol_rate,
                                       options.samples_per_symbol,
                                       options.rx_freq, options.rx_gain,
                                       options.spec, options.antenna,
                                       options.verbose)
            options.samples_per_symbol = self.source._sps

        elif(options.from_file is not None):
            sys.stderr.write(("Reading samples from '%s'.\n\n" % (options.from_file)))
            self.source = gr.file_source(gr.sizeof_gr_complex, options.from_file)
        else:
            sys.stderr.write("No source defined, pulling samples from null source.\n\n")
            self.source = gr.null_source(gr.sizeof_gr_complex)

        # Set up receive path
        # do this after for any adjustments to the options that may
        # occur in the sinks (specifically the UHD sink)
        self.rxpath = [ ]
        self.rxpath.append(receive_path(demodulator, rx_callback0, options))
        self.rxpath.append(receive_path(demodulator, rx_callback1, options))

        samp_rate = self.source.get_sample_rate()
        print "SAMP RATE " + str(samp_rate)
        
        band_transition = options.trans_width
        low_transition = options.trans_width

        self.band_pass_filter_qv0 = gr.fir_filter_ccc(1, firdes.complex_band_pass(
            1, samp_rate, 0e3, samp_rate/2, band_transition, firdes.WIN_HAMMING, 6.76))
        self.band_pass_filter_qv1 = gr.fir_filter_ccc(1, firdes.complex_band_pass(
            1, samp_rate, (-samp_rate/2), 0e3, band_transition, firdes.WIN_HAMMING, 6.76))

        self.freq_translate_qv0 = filter.freq_xlating_fir_filter_ccc(2, (2, ), -samp_rate/4, samp_rate)
        self.freq_translate_qv1 = filter.freq_xlating_fir_filter_ccc(2, (2, ), samp_rate/4, samp_rate)

        guard_region = options.guard_width

        self.low_pass_filter_qv0 = gr.fir_filter_ccf(1, firdes.low_pass(
            1, samp_rate/2, samp_rate/4-guard_region, low_transition, firdes.WIN_HAMMING, 6.76))
        self.low_pass_filter_qv1 = gr.fir_filter_ccf(1, firdes.low_pass(
            1, samp_rate/2, samp_rate/4-guard_region, low_transition, firdes.WIN_HAMMING, 6.76))

        self.connect(self.source, self.band_pass_filter_qv0)
        self.connect((self.band_pass_filter_qv0, 0), (self.freq_translate_qv0, 0))
        self.connect((self.freq_translate_qv0, 0), (self.low_pass_filter_qv0, 0))
        self.connect((self.low_pass_filter_qv0, 0), (self.rxpath[0], 0))

        self.connect(self.source, self.band_pass_filter_qv1)
        self.connect((self.band_pass_filter_qv1, 0), (self.freq_translate_qv1, 0))
        self.connect((self.freq_translate_qv1, 0), (self.low_pass_filter_qv1, 0))
        self.connect((self.low_pass_filter_qv1, 0), (self.rxpath[1], 0))
     

# /////////////////////////////////////////////////////////////////////////////
#                                   main
# /////////////////////////////////////////////////////////////////////////////

global n_rcvd, n_right

def main():
    global n_rcvd, n_right

    n_rcvd = 0
    n_right = 0
    
    def rx_callback0(ok, payload):
        global n_rcvd, n_right
        (pktno,) = struct.unpack('!H', payload[0:2])
        n_rcvd += 1
        if ok:
            n_right += 1

        print "ok = %5s  pktno = %4d  n_rcvd = %4d  n_right = %4d   channel = 0" % (
            ok, pktno, n_rcvd, n_right)

    def rx_callback1(ok, payload):
        global n_rcvd, n_right
        (pktno,) = struct.unpack('!H', payload[0:2])
        n_rcvd += 1
        if ok:
            n_right += 1

        print "ok = %5s  pktno = %4d  n_rcvd = %4d  n_right = %4d   channel = 1" % (
            ok, pktno, n_rcvd, n_right)


    demods = digital.modulation_utils.type_1_demods()

    # Create Options Parser:
    parser = OptionParser (option_class=eng_option, conflict_handler="resolve")
    expert_grp = parser.add_option_group("Expert")

    parser.add_option("-m", "--modulation", type="choice", choices=demods.keys(), 
                      default='psk',
                      help="Select modulation from: %s [default=%%default]"
                            % (', '.join(demods.keys()),))
    parser.add_option("","--from-file", default=None,
                      help="input file of samples to demod")
    
    custom_grp = parser.add_option_group("Custom")
    custom_grp.add_option("","--trans-width", type="eng_float", default=50e3,
                      help="transition width for low pass filter")
    custom_grp.add_option("","--guard-width", type="eng_float", default=10e3,
                      help="guard region width")
    custom_grp.add_option("","--file-samp-rate", type="eng_float", default=1e6,
                      help="file sample rate")
    custom_grp.add_option("","--split-amplitude", type="eng_float", default=0.08,
                      help="multiplier post split")

    receive_path.add_options(parser, expert_grp)
    uhd_receiver.add_options(parser)

    for mod in demods.values():
        mod.add_options(expert_grp)

    (options, args) = parser.parse_args ()

    if len(args) != 0:
        parser.print_help(sys.stderr)
        sys.exit(1)

    if options.from_file is None:
        if options.rx_freq is None:
            sys.stderr.write("You must specify -f FREQ or --freq FREQ\n")
            parser.print_help(sys.stderr)
            sys.exit(1)


    # build the graph
    tb = my_top_block(demods[options.modulation], rx_callback0, rx_callback1, options)

    r = gr.enable_realtime_scheduling()
    if r != gr.RT_OK:
        print "Warning: Failed to enable realtime scheduling."

    tb.start()        # start flow graph
    tb.wait()         # wait for it to finish

if __name__ == '__main__':
    try:
        main()
    except KeyboardInterrupt:
        pass
