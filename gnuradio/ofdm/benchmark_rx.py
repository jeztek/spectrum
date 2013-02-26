#!/usr/bin/env python
#
# Copyright 2006,2007,2011 Free Software Foundation, Inc.
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

from gnuradio import gr, blks2
from gnuradio import eng_notation
from gnuradio.eng_option import eng_option
from optparse import OptionParser

from uhd_interface import uhd_transmitter
from ofdm import ofdm_demod
from gnuradio import filter
from gnuradio.filter import firdes
from gnuradio.gr import firdes
from gnuradio import blocks
from gnuradio import eng_notation

# from current dir
from receive_path import receive_path
from uhd_interface import uhd_receiver

import struct, sys

class my_top_block(gr.top_block):
    def __init__(self, callback0, callback1, options):
        gr.top_block.__init__(self)
        use_source = None
        if(options.rx_freq is not None):
            self.source = uhd_receiver(options.args,
                                       options.bandwidth,
                                       options.rx_freq, options.rx_gain,
                                       options.spec, options.antenna,
                                       options.verbose)
            use_source = self.source
        elif(options.from_file is not None):
            self.source = gr.file_source(gr.sizeof_gr_complex, options.from_file)
            self.throttle = gr.throttle(gr.sizeof_gr_complex*1, options.file_samp_rate)
            self.connect(self.source, self.throttle)
            use_source = self.throttle
        else:
            self.source = gr.null_source(gr.sizeof_gr_complex)

        # Set up receive path
        # do this after for any adjustments to the options that may
        # occur in the sinks (specifically the UHD sink)
        self.rxpath = [ ]
        self.rxpath.append(receive_path(callback0, options))
        self.rxpath.append(receive_path(callback1, options))

        samp_rate = 0
        if(options.rx_freq is not None):
            samp_rate = self.source.get_sample_rate()
        else:
            samp_rate = options.file_samp_rate
      
        band_transition = options.trans_width
        low_transition = options.trans_width
        guard_region = options.guard_width
  
        self.band_pass_filter_qv0 = gr.fir_filter_ccc(1, firdes.complex_band_pass(
            1, samp_rate, 0e3, samp_rate/2, band_transition, firdes.WIN_HAMMING, 6.76))
        self.band_pass_filter_qv1 = gr.fir_filter_ccc(1, firdes.complex_band_pass(
            1, samp_rate, (-samp_rate/2), 0e3, band_transition, firdes.WIN_HAMMING, 6.76))

        self.freq_translate_qv0 = filter.freq_xlating_fir_filter_ccc(2, (2, ), -samp_rate/4, samp_rate)
        self.freq_translate_qv1 = filter.freq_xlating_fir_filter_ccc(2, (2, ), samp_rate/4, samp_rate)

        self.low_pass_filter_qv0 = gr.fir_filter_ccf(1, firdes.low_pass(
            1, samp_rate/2, samp_rate/4-guard_region, low_transition, firdes.WIN_HAMMING, 6.76))
        self.low_pass_filter_qv1 = gr.fir_filter_ccf(1, firdes.low_pass(
            1, samp_rate/2, samp_rate/4-guard_region, low_transition, firdes.WIN_HAMMING, 6.76))

        self.connect(use_source, self.band_pass_filter_qv0)
        self.connect((self.band_pass_filter_qv0, 0), (self.freq_translate_qv0, 0))
        self.connect((self.freq_translate_qv0, 0), (self.low_pass_filter_qv0, 0))
        self.connect((self.low_pass_filter_qv0, 0), (self.rxpath[0], 0))

        self.connect(use_source, self.band_pass_filter_qv1)
        self.connect((self.band_pass_filter_qv1, 0), (self.freq_translate_qv1, 0))
        self.connect((self.freq_translate_qv1, 0), (self.low_pass_filter_qv1, 0))
        self.connect((self.low_pass_filter_qv1, 0), (self.rxpath[1], 0))

# /////////////////////////////////////////////////////////////////////////////
#                                   main
# /////////////////////////////////////////////////////////////////////////////

def main():

    global n_rcvd, n_right
        
    n_rcvd = 0
    n_right = 0

    def rx_callback0(ok, payload):
        global n_rcvd, n_right
        n_rcvd += 1
        (pktno,) = struct.unpack('!H', payload[0:2])
        if ok:
            n_right += 1
        print "ok: %r \t pktno: %d \t n_rcvd: %d \t n_right: %d\t channel = 0" % (ok, pktno, n_rcvd, n_right)

    def rx_callback1(ok, payload):
        global n_rcvd, n_right
        n_rcvd += 1
        (pktno,) = struct.unpack('!H', payload[0:2])
        if ok:
            n_right += 1
        print "ok: %r \t pktno: %d \t n_rcvd: %d \t n_right: %d\t channel = 1" % (ok, pktno, n_rcvd, n_right)

    parser = OptionParser(option_class=eng_option, conflict_handler="resolve")
    expert_grp = parser.add_option_group("Expert")
    custom_grp = parser.add_option_group("Custom")
    parser.add_option("","--discontinuous", action="store_true", default=False,
                      help="enable discontinuous")
    parser.add_option("","--from-file", default=None,
                      help="input file of samples to demod")
    custom_grp.add_option("","--trans-width", type="eng_float", default=50e3,
                      help="transition width for low pass filter")
    custom_grp.add_option("","--guard-width", type="eng_float", default=10e3,
                      help="guard region width")
    custom_grp.add_option("","--file-samp-rate", type="eng_float", default=1e6,
                      help="file sample rate")
    custom_grp.add_option("","--split-amplitude", type="eng_float", default=1,
                      help="multiplier post split")
    custom_grp.add_option("","--rs-n", type="int", default=0,
                      help="reed solomon n")
    custom_grp.add_option("","--rs-k", type="int", default=0,
                      help="reed solomon k")
    receive_path.add_options(parser, expert_grp)
    uhd_receiver.add_options(parser)
    ofdm_demod.add_options(parser, expert_grp)

    (options, args) = parser.parse_args ()

    if options.from_file is None:
        if options.rx_freq is None:
            sys.stderr.write("You must specify -f FREQ or --freq FREQ\n")
            parser.print_help(sys.stderr)
            sys.exit(1)

    # build the graph
    tb = my_top_block(rx_callback0, rx_callback1, options)

    r = gr.enable_realtime_scheduling()
    if r != gr.RT_OK:
        print "Warning: failed to enable realtime scheduling"

    tb.start()                      # start flow graph
    tb.wait()                       # wait for it to finish

if __name__ == '__main__':
    try:
        main()
    except KeyboardInterrupt:
        pass
